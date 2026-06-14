using System.Text;
using RabbitMQ.Client;

namespace Facturacion.API.Eventing;

public sealed class RabbitMqEventBus : IEventBus
{
    private readonly RabbitMqConnection _connection;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly RabbitMqOptions _options;

    public RabbitMqEventBus(
        RabbitMqConnection connection,
        Microsoft.Extensions.Options.IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqEventBus> logger)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishJsonAsync(string routingKey, string payload, Guid eventId, Guid correlationId, string eventType, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var channel = _connection.CreateChannel();
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.DeliveryMode = 2;
        props.MessageId = eventId.ToString("D");
        props.CorrelationId = correlationId.ToString("D");
        props.Type = eventType;

        channel.BasicPublish(_options.ExchangeName, routingKey, false, props, Encoding.UTF8.GetBytes(payload));
        _logger.LogInformation(
            "Evento publicado desde Facturacion. RoutingKey={RoutingKey}, EventId={EventId}, CorrelationId={CorrelationId}, EventType={EventType}",
            routingKey,
            eventId,
            correlationId,
            eventType);
        return Task.CompletedTask;
    }
}
