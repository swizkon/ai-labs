using System.Diagnostics;

namespace FactoryObservability.Shared.Telemetry;

public static class W3CTraceContext
{
    private const string Version = "00";
    private const string Flags = "01";

    /// <summary>Build a W3C traceparent from the current activity context.</summary>
    public static string FormatTraceParent(ActivityContext context)
    {
        return $"{Version}-{context.TraceId}-{context.SpanId}-{Flags}";
    }

    /// <summary>Parse a W3C traceparent into <see cref="ActivityContext"/> for parenting consumer spans.</summary>
    public static bool TryParseTraceParent(string? traceParent, out ActivityContext parentContext)
    {
        parentContext = default;
        if (string.IsNullOrWhiteSpace(traceParent))
            return false;

        return ActivityContext.TryParse(traceParent, null, out parentContext);
    }
}
