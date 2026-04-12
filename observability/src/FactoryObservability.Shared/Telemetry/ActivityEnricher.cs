using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace FactoryObservability.Shared.Telemetry;

/// <summary>Adds W3C trace_id / span_id / parent_span_id from <see cref="Activity.Current"/>.</summary>
public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("trace_id", activity.TraceId.ToHexString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("span_id", activity.SpanId.ToHexString()));

        var parent = activity.Parent;
        if (parent is not null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("parent_span_id", parent.SpanId.ToHexString()));
        }
    }
}
