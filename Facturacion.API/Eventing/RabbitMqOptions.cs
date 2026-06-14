namespace Facturacion.API.Eventing;

public sealed class RabbitMqOptions
{
    public string? Uri { get; set; }
    public bool UseSsl { get; set; }
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "hotel.integration.events";
    public string DeadLetterExchangeName { get; set; } = "hotel.integration.dlx";
    public string FacturacionReservasQueue { get; set; } = "facturacion.reservas.queue";
    public string FacturacionReservasDlq { get; set; } = "facturacion.reservas.dlq";
}
