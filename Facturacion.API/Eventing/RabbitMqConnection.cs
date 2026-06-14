using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Facturacion.API.Eventing;

public sealed class RabbitMqConnection : IDisposable
{
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly RabbitMqOptions _options;
    private readonly object _syncRoot = new();
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IModel CreateChannel()
    {
        return GetConnection().CreateModel();
    }

    public bool CanConnect()
    {
        try
        {
            using var channel = CreateChannel();
            return channel.IsOpen;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ no disponible para Facturacion en {Host}:{Port}", _options.HostName, _options.Port);
            return false;
        }
    }

    private IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        lock (_syncRoot)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = CreateFactory();

            _connection = factory.CreateConnection("facturacion-api-eventbus");
            using var channel = _connection.CreateModel();
            DeclareTopology(channel);
            _logger.LogInformation("RabbitMQ conectado para Facturacion en {Host}:{Port}. Exchange={Exchange}", _options.HostName, _options.Port, _options.ExchangeName);
            return _connection;
        }
    }

    private ConnectionFactory CreateFactory()
    {
        if (!string.IsNullOrWhiteSpace(_options.Uri))
        {
            return new ConnectionFactory
            {
                Uri = new Uri(_options.Uri)
            };
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };

        if (_options.UseSsl)
        {
            factory.Ssl.Enabled = true;
            factory.Ssl.ServerName = _options.HostName;
        }

        return factory;
    }

    private void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(_options.DeadLetterExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.QueueDeclare(_options.FacturacionReservasDlq, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_options.FacturacionReservasDlq, _options.DeadLetterExchangeName, "#");

        var args = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _options.DeadLetterExchangeName,
            ["x-dead-letter-routing-key"] = _options.FacturacionReservasDlq
        };

        channel.QueueDeclare(_options.FacturacionReservasQueue, durable: true, exclusive: false, autoDelete: false, arguments: args);
        channel.QueueBind(_options.FacturacionReservasQueue, _options.ExchangeName, "reservas.reserva.confirmada.v1");
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
