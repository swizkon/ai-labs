using System.Diagnostics;
using System.Text.Json;
using BroadcastParser;
using FactoryObservability.Shared;
using FactoryObservability.Shared.Messaging;
using FactoryObservability.Shared.Telemetry;
using MongoDB.Driver;
using Serilog;

TelemetryBootstrap.RegisterActivityListeners();

var builder = WebApplication.CreateBuilder(args);
Log.Logger = SerilogBootstrap.CreateLogger(ServiceNames.BroadcastParser);
builder.Host.UseSerilog();

builder.Services.Configure<IbmMqOptions>(builder.Configuration.GetSection(IbmMqOptions.SectionName));
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var cs = builder.Configuration.GetConnectionString("Mongo")
             ?? throw new InvalidOperationException("ConnectionStrings:Mongo is required");
    return new MongoClient(cs);
});

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/broadcast", async (
    BroadcastRequest req,
    IMongoClient mongo,
    IConfiguration cfg,
    ILogger<Program> log,
    CancellationToken ct) =>
{
    if (!MixNumber.TryNormalize(req.MixNumber, out var mix, out var err))
        return Results.BadRequest(new { error = err });

    var swTotal = Stopwatch.StartNew();
    using var root = PipelineLog.StartActivity(FactoryActivitySources.BroadcastParser, "http_ingress", ActivityKind.Server);
    root?.SetTag("mix.number", mix);

    PipelineLog.Step(log, mix, PipelineSteps.HttpIngress, PipelineEvents.Start, "accepted broadcast request");

    try
    {
        var dbName = cfg["Mongo:Database"] ?? "factory";
        var db = mongo.GetDatabase(dbName);

        using (var _ = PipelineLog.StartActivity(FactoryActivitySources.BroadcastParser, "mongo_write", ActivityKind.Internal))
        {
            var sw = Stopwatch.StartNew();
            PipelineLog.Step(log, mix, PipelineSteps.MongoWrite, PipelineEvents.Start, "persist generated entities");
            var col = db.GetCollection<FactoryEntity>("entities");
            await col.InsertManyAsync(
                new[]
                {
                    new FactoryEntity { MixNumber = mix, EntityType = MessageTypes.VehicleObject, CreatedAtUtc = DateTime.UtcNow },
                    new FactoryEntity { MixNumber = mix, EntityType = MessageTypes.EndOfLineObject, CreatedAtUtc = DateTime.UtcNow }
                },
                cancellationToken: ct);
            sw.Stop();
            PipelineLog.Step(log, mix, PipelineSteps.MongoWrite, PipelineEvents.Complete, "inserted vehicleObject and endOfLineObject", sw.ElapsedMilliseconds);
        }

        var mqOpts = cfg.GetSection(IbmMqOptions.SectionName).Get<IbmMqOptions>() ?? new IbmMqOptions();
        using var qm = IbmMq.Connect(mqOpts);

        using (var pub = PipelineLog.StartActivity(FactoryActivitySources.BroadcastParser, "mq_publish_vehicle", ActivityKind.Internal))
        {
            pub?.SetTag("mix.number", mix);
            var sw = Stopwatch.StartNew();
            PipelineLog.Step(log, mix, PipelineSteps.MqPublish, PipelineEvents.Start, "publish vehicleObject to PIM queue");
            var tp = W3CTraceContext.FormatTraceParent(W3CTraceContext.ResolveContext(pub));
            IbmMq.PutJson(
                qm,
                mqOpts.PimQueue,
                new FactoryMqEnvelope
                {
                    MessageType = MessageTypes.VehicleObject,
                    MixNumber = mix,
                    TraceParent = tp,
                    Payload = new { entity = MessageTypes.VehicleObject, mix_number = mix }
                },
                ct);
            sw.Stop();
            PipelineLog.Step(log, mix, PipelineSteps.MqPublish, PipelineEvents.Complete, "published vehicleObject", sw.ElapsedMilliseconds);
        }

        using (var pub = PipelineLog.StartActivity(FactoryActivitySources.BroadcastParser, "mq_publish_eol", ActivityKind.Internal))
        {
            pub?.SetTag("mix.number", mix);
            var sw = Stopwatch.StartNew();
            PipelineLog.Step(log, mix, PipelineSteps.MqPublish, PipelineEvents.Start, "publish endOfLineObject to Instructions queue");
            var tp = W3CTraceContext.FormatTraceParent(W3CTraceContext.ResolveContext(pub));
            IbmMq.PutJson(
                qm,
                mqOpts.InstructionsQueue,
                new FactoryMqEnvelope
                {
                    MessageType = MessageTypes.EndOfLineObject,
                    MixNumber = mix,
                    TraceParent = tp,
                    Payload = new { entity = MessageTypes.EndOfLineObject, mix_number = mix }
                },
                ct);
            sw.Stop();
            PipelineLog.Step(log, mix, PipelineSteps.MqPublish, PipelineEvents.Complete, "published endOfLineObject", sw.ElapsedMilliseconds);
        }

        swTotal.Stop();
        PipelineLog.Step(log, mix, PipelineSteps.HttpIngress, PipelineEvents.Complete, "broadcast pipeline finished", swTotal.ElapsedMilliseconds);

        return Results.Ok(new
        {
            mix_number = mix,
            trace_id = Activity.Current?.TraceId.ToHexString()
        });
    }
    catch (Exception ex)
    {
        swTotal.Stop();
        PipelineLog.Step(log, mix, PipelineSteps.HttpIngress, PipelineEvents.Fail, "broadcast pipeline failed", swTotal.ElapsedMilliseconds, ex);
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.Run();
