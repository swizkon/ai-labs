namespace FactoryObservability.Shared;

/// <summary>Values for the <c>step</c> log field.</summary>
public static class PipelineSteps
{
    public const string HttpIngress = "http_ingress";
    public const string MongoWrite = "mongo_write";
    public const string MqPublish = "mq_publish";
    public const string MqConsume = "mq_consume";
    public const string InstructionLookup = "instruction_lookup";
    public const string PimProcess = "pim_process";
}
