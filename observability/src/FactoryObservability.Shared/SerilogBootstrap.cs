using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using FactoryObservability.Shared.Telemetry;

namespace FactoryObservability.Shared;

/// <summary>Console JSON logging (compact) with service name and <see cref="Activity"/> ids.</summary>
public static class SerilogBootstrap
{
    public static Serilog.ILogger CreateLogger(string serviceName)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service", serviceName)
            .Enrich.With<ActivityEnricher>()
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();
    }
}
