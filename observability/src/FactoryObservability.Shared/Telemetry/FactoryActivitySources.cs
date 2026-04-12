using System.Diagnostics;

namespace FactoryObservability.Shared.Telemetry;

/// <summary>One <see cref="ActivitySource"/> per deployable module (stable names for OTel later).</summary>
public static class FactoryActivitySources
{
    public static readonly ActivitySource BroadcastParser = new(ServiceNames.BroadcastParser);
    public static readonly ActivitySource PIMIntegrator = new(ServiceNames.PIMIntegrator);
    public static readonly ActivitySource InstructionsGenerator = new(ServiceNames.InstructionsGenerator);
}
