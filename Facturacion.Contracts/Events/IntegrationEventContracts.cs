namespace Facturacion.Contracts.Events;

public interface IIntegrationEvent
{
    Guid EventId { get; init; }
    DateTime OccurredOnUtc { get; init; }
    string EventType { get; }
    string EventVersion { get; }
    Guid CorrelationId { get; init; }
    Guid? CausationId { get; init; }
    string Source { get; init; }
    string? IdempotencyKey { get; init; }
    string SchemaVersion { get; }
}

public sealed record IntegrationEventEnvelope<TPayload>
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string EventVersion { get; init; } = "v1";
    public DateTime OccurredOnUtc { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? IdempotencyKey { get; init; }
    public string SchemaVersion { get; init; } = "1.0";
    public TPayload Payload { get; init; } = default!;
}

public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    public abstract string EventType { get; }
    public string EventVersion { get; init; } = "v1";
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public string Source { get; init; } = "facturacion-service";
    public string? IdempotencyKey { get; init; }
    public string SchemaVersion { get; init; } = "1.0";
}

