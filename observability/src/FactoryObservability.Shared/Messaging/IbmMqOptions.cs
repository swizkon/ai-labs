namespace FactoryObservability.Shared.Messaging;

public sealed class IbmMqOptions
{
    public const string SectionName = "IbmMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1414;
    public string Channel { get; set; } = "DEV.APP.SVRCONN";
    public string QueueManager { get; set; } = "QM1";
    public string User { get; set; } = "app";
    public string Password { get; set; } = "passw0rd";
    /// <summary>IBM MQ dev images pre-provision <c>DEV.QUEUE.1</c> etc. with <c>app</c> access.</summary>
    public string PimQueue { get; set; } = "DEV.QUEUE.1";

    public string InstructionsQueue { get; set; } = "DEV.QUEUE.2";
}
