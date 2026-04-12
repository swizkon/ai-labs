using System.Diagnostics;

namespace FactoryObservability.Shared.Telemetry;

/// <summary>
/// Ensures <see cref="ActivitySource.StartActivity"/> returns non-null activities when no other sampler is present
/// (important for generic hosts / worker services).
/// </summary>
public static class TelemetryBootstrap
{
    private static readonly object Gate = new();
    private static bool _registered;

    public static void RegisterActivityListeners()
    {
        lock (Gate)
        {
            if (_registered)
                return;

            _registered = true;
            ActivitySource.AddActivityListener(new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
            });
        }
    }
}
