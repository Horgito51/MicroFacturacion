namespace Facturacion.Contracts.Events;

public sealed record PagoRechazadoIntegrationEvent : IntegrationEventBase
{
    public override string EventType => "facturacion.pago.rechazado";
    public Guid FacturaGuid { get; init; }
    public Guid ReservaGuid { get; init; }
    public decimal MontoIntentado { get; init; }
    public string Moneda { get; init; } = "USD";
    public string MetodoPago { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
    public string? ProveedorPasarela { get; init; }
    public string? TransaccionExterna { get; init; }
    public DateTime FechaRechazoUtc { get; init; }
}

