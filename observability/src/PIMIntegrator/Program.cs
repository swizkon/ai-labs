using FactoryObservability.Shared;
using FactoryObservability.Shared.Messaging;
using FactoryObservability.Shared.Telemetry;
using Microsoft.Extensions.Hosting;
using PIMIntegrator;
using Serilog;

TelemetryBootstrap.RegisterActivityListeners();

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
Log.Logger = SerilogBootstrap.CreateLogger(ServiceNames.PIMIntegrator);
builder.Services.AddSerilog(Log.Logger);
builder.Services.Configure<IbmMqOptions>(builder.Configuration.GetSection(IbmMqOptions.SectionName));
builder.Services.AddHostedService<PimWorker>();

var host = builder.Build();
await host.RunAsync();
