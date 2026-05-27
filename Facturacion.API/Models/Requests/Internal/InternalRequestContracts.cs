using Facturacion.Business.DTOs.Facturacion;

namespace Facturacion.API.Models.Requests.Internal
{
    public sealed class PagoCreateRequest
    {
        public int IdFactura { get; set; }
        public int IdReserva { get; set; }
        public decimal Monto { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public bool EsPagoElectronico { get; set; }
        public string ProveedorPasarela { get; set; } = string.Empty;
        public string? TransaccionExterna { get; set; }
        public string? CodigoAutorizacion { get; set; }
        public string? Referencia { get; set; }
        public string Moneda { get; set; } = "USD";
        public decimal TipoCambio { get; set; } = 1;
    }

    public sealed class PagoSimularRequest
    {
        public int IdReserva { get; set; }
        public decimal Monto { get; set; }
        public string TokenPago { get; set; } = string.Empty;
        public string? Referencia { get; set; }
    }

    public sealed class PagoEstadoRequest
    {
        public string NuevoEstado { get; set; } = string.Empty;
    }

    public sealed class AnularFacturaRequest
    {
        public string Motivo { get; set; } = string.Empty;
    }

    public static class InternalRequestMapper
    {
        public static PagoDTO ToDto(this PagoCreateRequest request) => new()
        {
            IdFactura = request.IdFactura,
            IdReserva = request.IdReserva,
            Monto = request.Monto,
            MetodoPago = request.MetodoPago,
            EsPagoElectronico = request.EsPagoElectronico,
            ProveedorPasarela = request.ProveedorPasarela,
            TransaccionExterna = request.TransaccionExterna ?? string.Empty,
            CodigoAutorizacion = request.CodigoAutorizacion ?? string.Empty,
            Referencia = request.Referencia ?? string.Empty,
            EstadoPago = "APR",
            Moneda = request.Moneda,
            TipoCambio = request.TipoCambio,
            CreadoPorUsuario = "Sistema",
            ServicioOrigen = "Facturacion"
        };
    }
}
