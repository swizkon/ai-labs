using FactoryObservability.Shared;
using FactoryObservability.Shared.Messaging;
using FactoryObservability.Shared.Telemetry;
using InstructionsGenerator;
using Microsoft.Extensions.Hosting;
using Serilog;

TelemetryBootstrap.RegisterActivityListeners();

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
Log.Logger = SerilogBootstrap.CreateLogger(ServiceNames.InstructionsGenerator);
builder.Services.AddSerilog(Log.Logger);
builder.Services.Configure<IbmMqOptions>(builder.Configuration.GetSection(IbmMqOptions.SectionName));
builder.Services.AddHostedService<InstructionsWorker>();

var host = builder.Build();
await host.RunAsync();
