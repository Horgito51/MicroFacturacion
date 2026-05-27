namespace Facturacion.API.Models.Responses.Public
{
    public sealed class PagoSimuladoPublicDto
    {
        public Guid ReservaGuid { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string EstadoPago { get; set; } = string.Empty;
        public string EstadoReserva { get; set; } = string.Empty;
        public string TransaccionExterna { get; set; } = string.Empty;
        public string CodigoAutorizacion { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public DateTime FechaPagoUtc { get; set; }
    }
}
