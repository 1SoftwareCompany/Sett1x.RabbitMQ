using Microsoft.Extensions.Options;
using One.Settix.RabbitMQ.Bootstrap.Management.Model;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace One.Settix.RabbitMQ.Bootstrap.Management
{
    internal sealed class RabbitMqManagementClient
    {
        private static readonly Regex UrlRegex = new Regex(@"^(http|https):\/\/.+\w$");

        readonly int portNumber;
        readonly bool useSsl;
        readonly int sslEnabledPort = 443;
        readonly int sslDisabledPort = 15672;
        readonly JsonSerializerOptions settings;

        private readonly HttpClient _httpClient;

        private readonly List<string> apiAddressCollection;
        private string lastKnownApiAddress;

        internal RabbitMqManagementClient(IHttpClientFactory httpClientFactory, RabbitMqOptions settings) : this(httpClientFactory, settings.ApiAddress ?? settings.Server, settings.Username, settings.Password, useSsl: settings.UseSsl) { }

        internal RabbitMqManagementClient(IHttpClientFactory httpClientFactory, string apiAddresses, string username, string password, bool useSsl = false, TimeSpan? timeout = null)
        {
            if (httpClientFactory == null) throw new ArgumentNullException(nameof(httpClientFactory));
            _httpClient = httpClientFactory.CreateClient();

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("RabbitMQ username is null or empty.");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("RabbitMQ password is null or empty.");

            _httpClient.Timeout = timeout ?? TimeSpan.FromSeconds(20);

            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            portNumber = useSsl ? sslEnabledPort : sslDisabledPort;
            this.useSsl = useSsl;
            apiAddressCollection = new List<string>();

            string[] parsedAddresses = apiAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var apiAddress in parsedAddresses)
            {
                TryInitializeApiHostName(apiAddress, useSsl);
            }
            if (apiAddressCollection.Any() == false) throw new ArgumentException("Invalid API addresses", nameof(apiAddresses));

            settings = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        private void TryInitializeApiHostName(string address, bool useSsl)
        {
            string result = $"{address.Trim()}:{portNumber}";

            if (string.IsNullOrEmpty(result)) return;

            string schema = useSsl ? "https://" : "http://";

            result = result.Contains(schema) ? result : schema + result;

            if (UrlRegex.IsMatch(result) && Uri.TryCreate(result, UriKind.Absolute, out _))
            {
                apiAddressCollection.Add(result);
            }
        }

        internal async Task<Vhost> CreateVirtualHostAsync(string virtualHostName)
        {
            if (string.IsNullOrEmpty(virtualHostName)) throw new ArgumentException("virtualHostName is null or empty");

            await PutAsync($"vhosts/{virtualHostName}").ConfigureAwait(false);

            return await GetVhostAsync(virtualHostName).ConfigureAwait(false);
        }

        internal Task<Vhost> GetVhostAsync(string vhostName)
        {
            string vhost = SanitiseVhostName(vhostName);
            return GetAsync<Vhost>($"vhosts/{vhost}");
        }

        internal Task<IEnumerable<Vhost>> GetVHostsAsync()
        {
            return GetAsync<IEnumerable<Vhost>>("vhosts");
        }

        internal async Task CreatePermissionAsync(PermissionInfo permissionInfo)
        {
            if (permissionInfo is null) throw new ArgumentNullException("permissionInfo");

            string vhost = SanitiseVhostName(permissionInfo.GetVirtualHostName());
            string username = permissionInfo.GetUserName();
            await PutAsync($"permissions/{vhost}/{username}", permissionInfo).ConfigureAwait(false);
        }

        internal async Task CreateFederatedExchangeAsync(FederatedExchange exchange, string ownerVhost)
        {
            await PutAsync($"parameters/federation-upstream/{ownerVhost}/{exchange.Name}", exchange).ConfigureAwait(false);
        }

        internal async Task CreatePolicyAsync(Policy policy, string ownerVhost)
        {
            await PutAsync($"policies/{ownerVhost}/{policy.Name}", policy).ConfigureAwait(false);
        }

        internal Task<IEnumerable<User>> GetUsersAsync()
        {
            return GetAsync<IEnumerable<User>>("users");
        }

        internal Task<User> GetUserAsync(string userName)
        {
            return GetAsync<User>(string.Format("users/{0}", userName));
        }

        internal async Task<User> CreateUserAsync(UserInfo userInfo)
        {
            if (userInfo is null) throw new ArgumentNullException("userInfo");

            string username = userInfo.GetName();

            await PutAsync($"users/{username}", userInfo).ConfigureAwait(false);

            return await GetUserAsync(userInfo.GetName()).ConfigureAwait(false);
        }

        private async Task PutAsync(string path)
        {
            var requestUri = await BuildEndpointAddress(path);
            var response = await _httpClient.PutAsync(requestUri, new StringContent("", Encoding.UTF8, "application/json"));
            EnsureSuccess(response);
        }

        private async Task PutAsync<T>(string path, T item)
        {
            var requestUri = await BuildEndpointAddress(path);
            var json = JsonSerializer.Serialize(item, settings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(requestUri, content);
            EnsureSuccess(response);
        }

        private async Task<T> GetAsync<T>(string path, params object[] queryObjects)
        {
            var requestUri = await BuildEndpointAddress(path) + BuildQueryString(queryObjects);
            var response = await _httpClient.GetAsync(requestUri);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new UnexpectedHttpStatusCodeException(response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseBody, settings)!;
        }

        private string SanitiseVhostName(string vhostName) => vhostName.Replace("/", "%2f");

        private async Task<string> BuildEndpointAddress(string path)
        {
            if (string.IsNullOrEmpty(lastKnownApiAddress) == false)
            {
                if (await IsHostResponding(lastKnownApiAddress))
                    return string.Format("{0}/api/{1}", lastKnownApiAddress, path);
            }

            foreach (var apiAddress in apiAddressCollection)
            {
                if (await IsHostResponding(apiAddress))
                {
                    lastKnownApiAddress = apiAddress;
                    return string.Format("{0}/api/{1}", apiAddress, path);
                }
            }

            throw new Exception("Unable to connect to any of the provided API hosts.");
        }

        private async Task<bool> IsHostResponding(string address)
        {
            try
            {
                var response = await _httpClient.GetAsync(address);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private string BuildQueryString(object[] queryObjects)
        {
            if (queryObjects == null || queryObjects.Length == 0)
                return string.Empty;

            StringBuilder queryStringBuilder = new StringBuilder("?");
            var first = true;
            // One or more query objects can be used to build the query
            foreach (var query in queryObjects)
            {
                if (query == null)
                    continue;
                // All public properties are added to the query on the format property_name=value
                var type = query.GetType();
                foreach (var prop in type.GetProperties())
                {
                    var name = Regex.Replace(prop.Name, "([a-z])([A-Z])", "$1_$2").ToLower();
                    var value = prop.GetValue(query, null);
                    if (!first)
                    {
                        queryStringBuilder.Append("&");
                    }
                    queryStringBuilder.AppendFormat("{0}={1}", name, value ?? string.Empty);
                    first = false;
                }
            }
            return queryStringBuilder.ToString();
        }

        private void EnsureSuccess(HttpResponseMessage response)
        {
            if (!(response.StatusCode == HttpStatusCode.OK ||
                  response.StatusCode == HttpStatusCode.Created ||
                  response.StatusCode == HttpStatusCode.NoContent))
            {
                throw new UnexpectedHttpStatusCodeException(response.StatusCode);
            }
        }
    }
}
