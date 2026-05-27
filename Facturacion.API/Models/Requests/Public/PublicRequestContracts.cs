namespace Facturacion.API.Models.Requests.Public
{
    public sealed class PublicPagoSimularRequest
    {
        public Guid ReservaGuid { get; set; }
        public decimal Monto { get; set; }
        public string TokenPago { get; set; } = string.Empty;
        public string? Referencia { get; set; }

        public void ValidateNoIds()
        {
        }
    }
}
