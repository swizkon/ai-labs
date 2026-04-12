using System.Diagnostics;
using System.Text.Json;
using FactoryObservability.Shared;
using FactoryObservability.Shared.Messaging;
using FactoryObservability.Shared.Telemetry;
using Microsoft.Extensions.Options;

namespace InstructionsGenerator;

public sealed class InstructionsWorker : BackgroundService
{
    private readonly ILogger<InstructionsWorker> _log;
    private readonly IbmMqOptions _mq;

    public InstructionsWorker(ILogger<InstructionsWorker> log, IOptions<IbmMqOptions> mq)
    {
        _log = log;
        _mq = mq.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
        using var qm = IbmMq.Connect(_mq);
        using var queue = IbmMq.OpenInput(qm, _mq.InstructionsQueue);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!IbmMq.TryGetJson(queue, 1500, out var json, out _, out _))
                    continue;

                var env = JsonSerializer.Deserialize<FactoryMqEnvelope>(json, JsonOptions.Options);
                if (env is null || string.IsNullOrEmpty(env.MixNumber))
                    continue;

                var mix = env.MixNumber;
                var tp = env.TraceParent;
                ActivityContext? parent = W3CTraceContext.TryParseTraceParent(tp, out var ctx) ? ctx : null;

                using var consume = PipelineLog.StartActivity(
                    FactoryActivitySources.InstructionsGenerator,
                    "mq_consume",
                    ActivityKind.Consumer,
                    parent);

                var sw = Stopwatch.StartNew();
                PipelineLog.Step(_log, mix, PipelineSteps.MqConsume, PipelineEvents.Start, $"received {env.MessageType} from {_mq.InstructionsQueue}");

                using (var _ = PipelineLog.StartActivity(FactoryActivitySources.InstructionsGenerator, "instruction_lookup", ActivityKind.Internal))
                {
                    var swLookup = Stopwatch.StartNew();
                    PipelineLog.Step(_log, mix, PipelineSteps.InstructionLookup, PipelineEvents.Start, "resolve instructions for entities");
                    await Task.Delay(TimeSpan.FromMilliseconds(60), stoppingToken);
                    swLookup.Stop();
                    PipelineLog.Step(
                        _log,
                        mix,
                        PipelineSteps.InstructionLookup,
                        PipelineEvents.Complete,
                        "instructions resolved (created=2 skipped=0)",
                        swLookup.ElapsedMilliseconds);
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
                _log.LogError(ex, "Instructions worker loop error");
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
