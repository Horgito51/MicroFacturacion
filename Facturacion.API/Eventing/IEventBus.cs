namespace Facturacion.API.Eventing;

public interface IEventBus
{
    Task PublishJsonAsync(string routingKey, string payload, Guid eventId, Guid correlationId, string eventType, CancellationToken ct = default);
}
