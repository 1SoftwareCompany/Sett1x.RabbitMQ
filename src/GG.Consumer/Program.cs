using GG.Consumer;
using One.Settix.RabbitMQ;
using One.Settix.RabbitMQ.SettixConfigurationMessageProcessors;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddLogging();
builder.Services.AddSettix();
builder.Services.AddSingleton<ISettixConfigurationMessageProcessor, TestProcessor>();
builder.Services.AddHttpClients();

var host = builder.Build();

host.Run();
