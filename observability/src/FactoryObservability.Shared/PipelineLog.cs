using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FactoryObservability.Shared;

/// <summary>Structured pipeline logs aligned with docs/SCHEMA.md. <c>service</c> comes from Serilog enricher.</summary>
public static class PipelineLog
{
    public static void Step(
        ILogger logger,
        string mixNumber,
        string step,
        string pipelineEvent,
        string message,
        long? durationMs = null,
        Exception? error = null)
    {
        var isStart = string.Equals(pipelineEvent, PipelineEvents.Start, StringComparison.Ordinal);

        if (error is null)
        {
            if (isStart)
            {
                logger.LogInformation(
                    "{mix_number} {step} {event} {message}",
                    mixNumber,
                    step,
                    pipelineEvent,
                    message);
            }
            else
            {
                logger.LogInformation(
                    "{mix_number} {step} {event} {duration_ms} {message}",
                    mixNumber,
                    step,
                    pipelineEvent,
                    durationMs,
                    message);
            }
        }
        else
        {
            logger.LogError(
                error,
                "{mix_number} {step} {event} {duration_ms} {message} {error}",
                mixNumber,
                step,
                pipelineEvent,
                durationMs,
                message,
                error.Message);
        }
    }

    public static Activity? StartActivity(
        ActivitySource source,
        string name,
        ActivityKind kind,
        ActivityContext? parentContext = null)
    {
        return parentContext is { } ctx
            ? source.StartActivity(name, kind, ctx)
            : source.StartActivity(name, kind);
    }
}
