namespace Facturacion.DataAccess.Entities.Eventing;

public sealed class OutboxMessageEntity
{
    public long IdOutboxMessage { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventVersion { get; set; } = "v1";
    public string RoutingKey { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public Guid? CausationId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public DateTime OccurredOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? PublishedOnUtc { get; set; }
    public int PublishAttempts { get; set; }
    public string Status { get; set; } = "PEN";
    public string? LastError { get; set; }
}
