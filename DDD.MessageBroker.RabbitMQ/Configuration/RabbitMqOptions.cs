namespace DDD.MessageBroker.RabbitMQ.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "ddd";
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";

    public string Exchange { get; set; } = "ddd.events";
    public string Queue { get; set; } = "ddd.events.queue";
    public string RoutingKey { get; set; } = "ddd.events";

    public string RetryExchange { get; set; } = "ddd.events.retry";
    public string RetryQueue { get; set; } = "ddd.events.retry.queue";
    public string RetryRoutingKey { get; set; } = "ddd.events.retry";
    public int RetryDelayMilliseconds { get; set; } = 5000;
    public int MaxRetryCount { get; set; } = 3;

    public string DeadLetterExchange { get; set; } = "ddd.events.dlx";
    public string DeadLetterQueue { get; set; } = "ddd.events.dlq";
    public string DeadLetterRoutingKey { get; set; } = "ddd.events.dlq";

    public string InProgressExchange { get; set; } = "ddd.events.inprogress";
    public string InProgressQueue { get; set; } = "ddd.events.inprogress.queue";
    public string InProgressRoutingKey { get; set; } = "ddd.events.inprogress";

    public int InProgressDelayBufferMilliseconds { get; set; } = 250;
}