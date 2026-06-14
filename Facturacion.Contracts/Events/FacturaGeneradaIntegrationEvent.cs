namespace Facturacion.Contracts.Events;

public sealed record FacturaGeneradaIntegrationEvent : IntegrationEventBase
{
    public override string EventType => "facturacion.factura.generada";
    public Guid FacturaGuid { get; init; }
    public Guid ReservaGuid { get; init; }
    public string NumeroFactura { get; init; } = string.Empty;
    public string TipoFactura { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal ValorIva { get; init; }
    public decimal Total { get; init; }
    public decimal Saldo { get; init; }
    public string EstadoFactura { get; init; } = string.Empty;
}

