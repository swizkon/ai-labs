using System.Collections;
using System.Text;
using System.Text.Json;
using IBM.WMQ;

namespace FactoryObservability.Shared.Messaging;

/// <summary>Minimal IBM MQ put/get helpers for the PoC.</summary>
public static class IbmMq
{
    public static MQQueue OpenInput(MQQueueManager qm, string queueName) =>
        qm.AccessQueue(queueName, MQC.MQOO_INPUT_SHARED);

    public static MQQueueManager Connect(IbmMqOptions o)
    {
        var p = new Hashtable
        {
            { MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED },
            { MQC.HOST_NAME_PROPERTY, o.Host },
            { MQC.CHANNEL_PROPERTY, o.Channel },
            { MQC.PORT_PROPERTY, o.Port },
            { MQC.USER_ID_PROPERTY, o.User },
            { MQC.PASSWORD_PROPERTY, o.Password }
        };

        return new MQQueueManager(o.QueueManager, p);
    }

    public static void PutJson(
        MQQueueManager qm,
        string queueName,
        FactoryMqEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var json = JsonSerializer.Serialize(envelope, JsonSerializerOptionsCache.Options);
        var queue = qm.AccessQueue(queueName, MQC.MQOO_OUTPUT);
        try
        {
            var msg = new MQMessage
            {
                Format = MQC.MQFMT_STRING,
                CharacterSet = 1208
            };
            msg.WriteString(json);
            msg.SetStringProperty(MqPropertyNames.TraceParent, envelope.TraceParent);
            msg.SetStringProperty(MqPropertyNames.MixNumber, envelope.MixNumber);
            var pmo = new MQPutMessageOptions();
            queue.Put(msg, pmo);
        }
        finally
        {
            queue.Close();
        }
    }

    /// <summary>Blocking get with wait. Returns false on timeout.</summary>
    public static bool TryGetJson(
        MQQueue queue,
        int waitMs,
        out string json,
        out string? traceParent,
        out string? mixNumber)
    {
        json = string.Empty;
        traceParent = null;
        mixNumber = null;

        var msg = new MQMessage();
        var gmo = new MQGetMessageOptions
        {
            WaitInterval = waitMs,
            Options = MQC.MQGMO_WAIT | MQC.MQGMO_NO_SYNCPOINT
        };

        try
        {
            queue.Get(msg, gmo);
        }
        catch (MQException ex) when (ex.Reason == MQC.MQRC_NO_MSG_AVAILABLE)
        {
            return false;
        }

        msg.Seek(0);
        var len = msg.MessageLength;
        var buf = new byte[len];
        msg.ReadFully(ref buf, 0, len);
        json = Encoding.UTF8.GetString(buf);

        try
        {
            traceParent = msg.GetStringProperty(MqPropertyNames.TraceParent);
        }
        catch
        {
            /* optional */
        }

        try
        {
            mixNumber = msg.GetStringProperty(MqPropertyNames.MixNumber);
        }
        catch
        {
            /* optional */
        }

        return true;
    }

    private static class JsonSerializerOptionsCache
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
}
