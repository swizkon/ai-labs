using System.Diagnostics;
using System.Text.Json;
using FactoryObservability.Shared;
using FactoryObservability.Shared.Messaging;
using FactoryObservability.Shared.Telemetry;
using Microsoft.Extensions.Options;

namespace PIMIntegrator;

public sealed class PimWorker : BackgroundService
{
    private readonly ILogger<PimWorker> _log;
    private readonly IbmMqOptions _mq;

    public PimWorker(ILogger<PimWorker> log, IOptions<IbmMqOptions> mq)
    {
        _log = log;
        _mq = mq.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
        using var qm = IbmMq.Connect(_mq);
        using var queue = IbmMq.OpenInput(qm, _mq.PimQueue);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!IbmMq.TryGetJson(queue, 1500, out var json, out _, out _))
                {
                    continue;
                }

                var env = JsonSerializer.Deserialize<FactoryMqEnvelope>(json, JsonOptions.Options);
                if (env is null || string.IsNullOrEmpty(env.MixNumber))
                    continue;

                var mix = env.MixNumber;
                var tp = env.TraceParent;
                ActivityContext? parent = null;
                if (!string.IsNullOrEmpty(tp) && W3CTraceContext.TryParseTraceParent(tp, out var ctx))
                    parent = ctx;

                using var consume = PipelineLog.StartActivity(
                    FactoryActivitySources.PIMIntegrator,
                    "mq_consume",
                    ActivityKind.Consumer,
                    parent);

                var sw = Stopwatch.StartNew();
                PipelineLog.Step(_log, mix, PipelineSteps.MqConsume, PipelineEvents.Start, $"received {env.MessageType} from {_mq.PimQueue}");

                using (var pim = PipelineLog.StartActivity(FactoryActivitySources.PIMIntegrator, "pim_process", ActivityKind.Internal))
                {
                    var swPim = Stopwatch.StartNew();
                    PipelineLog.Step(_log, mix, PipelineSteps.PimProcess, PipelineEvents.Start, "PIM integration");
                    await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
                    swPim.Stop();
                    PipelineLog.Step(_log, mix, PipelineSteps.PimProcess, PipelineEvents.Complete, "PIM integration finished", swPim.ElapsedMilliseconds);
                }

                sw.Stop();
                PipelineLog.Step(_log, mix, PipelineSteps.MqConsume, PipelineEvents.Complete, "message handled", sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "PIM worker loop error");
            }
        }
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
