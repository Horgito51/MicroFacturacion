namespace Facturacion.Contracts.Events;

public sealed record PagoRegistradoIntegrationEvent : IntegrationEventBase
{
    public override string EventType => "facturacion.pago.registrado";
    public Guid PagoGuid { get; init; }
    public Guid FacturaGuid { get; init; }
    public Guid ReservaGuid { get; init; }
    public decimal Monto { get; init; }
    public string Moneda { get; init; } = "USD";
    public string MetodoPago { get; init; } = string.Empty;
    public string EstadoPago { get; init; } = string.Empty;
    public string? ProveedorPasarela { get; init; }
    public string? TransaccionExterna { get; init; }
    public DateTime FechaPagoUtc { get; init; }
}

