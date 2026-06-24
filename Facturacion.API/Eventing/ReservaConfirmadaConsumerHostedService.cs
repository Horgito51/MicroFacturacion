using System.Text;
using System.Text.Json;
using Facturacion.Contracts.Events;
using Facturacion.DataAccess.Context;
using Facturacion.DataAccess.Entities.Eventing;
using Facturacion.DataAccess.Entities.Facturacion;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Reservas.Contracts.Events;

namespace Facturacion.API.Eventing;

public sealed class ReservaConfirmadaConsumerHostedService : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly ILogger<ReservaConfirmadaConsumerHostedService> _logger;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private IModel? _channel;

    public ReservaConfirmadaConsumerHostedService(
        RabbitMqConnection connection,
        Microsoft.Extensions.Options.IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ReservaConfirmadaConsumerHostedService> logger)
    {
        _connection = connection;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _channel = _connection.CreateChannel();
                _channel.BasicQos(0, 10, false);
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (_, args) => HandleMessage(args, stoppingToken);
                _channel.BasicConsume(_options.FacturacionReservasQueue, autoAck: false, consumer);
                _logger.LogInformation("Facturacion escuchando {Queue}", _options.FacturacionReservasQueue);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No fue posible iniciar consumer RabbitMQ de Facturacion. Reintentando en 10 segundos.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private void HandleMessage(BasicDeliverEventArgs args, CancellationToken ct)
    {
        try
        {
            if (args.RoutingKey != "reservas.reserva.confirmada.v1")
            {
                _channel?.BasicAck(args.DeliveryTag, false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FacturacionDbContext>();
            var body = Encoding.UTF8.GetString(args.Body.ToArray());
            var evt = JsonSerializer.Deserialize<ReservaConfirmadaIntegrationEvent>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("No se pudo deserializar ReservaConfirmadaIntegrationEvent.");

            _logger.LogInformation(
                "Evento recibido en Facturacion. RoutingKey={RoutingKey}, EventId={EventId}, CorrelationId={CorrelationId}",
                args.RoutingKey,
                evt.EventId,
                evt.CorrelationId);

            if (!RegisterInbox(db, evt.EventId, evt.EventType, evt.EventVersion, evt.Source, evt.CorrelationId))
            {
                _channel?.BasicAck(args.DeliveryTag, false);
                return;
            }

            var existing = db.Facturas
                .Include(f => f.Pagos)
                .FirstOrDefault(f =>
                f.ReservaGuid == evt.ReservaGuid &&
                f.TipoFactura == "RESERVA" &&
                f.Estado != "ANU" &&
                !f.EsEliminado);

            if (existing is null)
            {
                existing = CreateFacturaReserva(db, evt);
            }

            EnsurePagoSimulado(db, existing, evt);
            AddFacturaGeneradaOutbox(db, existing, evt);
            MarkInboxProcessed(db, evt.EventId);
            db.SaveChanges();

            _channel?.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consumiendo ReservaConfirmada en Facturacion. RoutingKey={RoutingKey}", args.RoutingKey);
            _channel?.BasicNack(args.DeliveryTag, false, requeue: false);
        }
    }

    private static FacturaEntity CreateFacturaReserva(FacturacionDbContext db, ReservaConfirmadaIntegrationEvent evt)
    {
        var subtotal = Math.Round(evt.TotalReserva / 1.12m, 2);
        var iva = Math.Round(evt.TotalReserva - subtotal, 2);
        var now = DateTime.UtcNow;
        var factura = new FacturaEntity
        {
            GuidFactura = Guid.NewGuid(),
            ClienteGuid = evt.ClienteGuid == Guid.Empty ? null : evt.ClienteGuid,
            ReservaGuid = evt.ReservaGuid,
            SucursalGuid = evt.SucursalGuid == Guid.Empty ? null : evt.SucursalGuid,
            IdCliente = ToPositiveLogicalId(evt.ClienteGuid),
            IdReserva = ToPositiveLogicalId(evt.ReservaGuid),
            IdSucursal = ToPositiveLogicalId(evt.SucursalGuid),
            NumeroFactura = $"EVT-{now:yyyyMMddHHmmss}-{evt.ReservaGuid.ToString("N")[..8]}",
            TipoFactura = "RESERVA",
            FechaEmision = now,
            Subtotal = subtotal,
            ValorIva = iva,
            DescuentoTotal = 0,
            Total = evt.TotalReserva,
            SaldoPendiente = evt.SaldoPendiente,
            Moneda = "USD",
            ObservacionesFactura = $"Factura generada por evento de reserva {evt.CodigoReserva}.",
            OrigenCanalFactura = "EVENTBUS",
            Estado = evt.SaldoPendiente <= 0 ? "PAG" : "EMI",
            EsEliminado = false,
            CreadoPorUsuario = "eventbus",
            FechaRegistroUtc = now,
            ServicioOrigen = "facturacion-service",
            FacturaDetalles = new List<FacturaDetalleEntity>()
        };

        factura.FacturaDetalles.Add(new FacturaDetalleEntity
        {
            FacturaDetalleGuid = Guid.NewGuid(),
            TipoItem = "ALOJAMIENTO",
            ReferenciaTipo = "RESERVA",
            DescripcionItem = $"Reserva {evt.CodigoReserva}",
            Cantidad = 1,
            PrecioUnitario = subtotal,
            SubtotalLinea = subtotal,
            ValorIvaLinea = iva,
            DescuentoLinea = 0,
            TotalLinea = evt.TotalReserva,
            FechaRegistroUtc = now,
            CreadoPorUsuario = "eventbus"
        });

        db.Facturas.Add(factura);
        db.SaveChanges();
        return factura;
    }

    private static void EnsurePagoSimulado(FacturacionDbContext db, FacturaEntity factura, ReservaConfirmadaIntegrationEvent evt)
    {
        var montoPago = factura.SaldoPendiente > 0
            ? factura.SaldoPendiente
            : factura.Total;

        if (montoPago <= 0)
        {
            factura.SaldoPendiente = 0;
            factura.Estado = "PAG";
            factura.ModificadoPorUsuario = "eventbus";
            factura.FechaModificacionUtc = DateTime.UtcNow;
            return;
        }

        var transaccionExterna = $"SIM-{evt.ReservaGuid:N}";
        var pago = db.Pagos.FirstOrDefault(p => p.TransaccionExterna == transaccionExterna);

        if (pago is null)
        {
            var now = DateTime.UtcNow;
            pago = new PagoEntity
            {
                PagoGuid = Guid.NewGuid(),
                IdFactura = factura.IdFactura,
                IdReserva = factura.IdReserva > 0 ? factura.IdReserva : ToPositiveLogicalId(evt.ReservaGuid),
                ReservaGuid = evt.ReservaGuid,
                Monto = montoPago,
                MetodoPago = "SIMULADO",
                EsPagoElectronico = true,
                ProveedorPasarela = "SIMULADOR",
                TransaccionExterna = transaccionExterna,
                CodigoAutorizacion = $"AUTH-{evt.ReservaGuid.ToString("N")[..12]}",
                Referencia = $"Pago simulado reserva {evt.CodigoReserva}",
                EstadoPago = "APR",
                FechaPagoUtc = now,
                Moneda = factura.Moneda,
                TipoCambio = 1.0000m,
                RespuestaPasarela = "Pago aprobado automaticamente por flujo v2 RabbitMQ.",
                CreadoPorUsuario = "eventbus",
                FechaRegistroUtc = now,
                ServicioOrigen = "facturacion-service"
            };

            db.Pagos.Add(pago);
        }

        factura.SaldoPendiente = 0;
        factura.Estado = "PAG";
        factura.ModificadoPorUsuario = "eventbus";
        factura.FechaModificacionUtc = DateTime.UtcNow;

        AddPagoRegistradoOutbox(db, pago, factura, evt);
    }

    private static void AddPagoRegistradoOutbox(
        FacturacionDbContext db,
        PagoEntity pago,
        FacturaEntity factura,
        ReservaConfirmadaIntegrationEvent sourceEvent)
    {
        var idempotencyKey = $"pago-registrado:{pago.PagoGuid:N}";
        if (db.OutboxMessages.Any(message =>
            message.EventType == "facturacion.pago.registrado" &&
            message.IdempotencyKey == idempotencyKey))
        {
            return;
        }

        var evt = new PagoRegistradoIntegrationEvent
        {
            PagoGuid = pago.PagoGuid,
            FacturaGuid = factura.GuidFactura,
            ReservaGuid = sourceEvent.ReservaGuid,
            Monto = pago.Monto,
            Moneda = pago.Moneda,
            MetodoPago = pago.MetodoPago,
            EstadoPago = pago.EstadoPago,
            ProveedorPasarela = pago.ProveedorPasarela,
            TransaccionExterna = pago.TransaccionExterna,
            FechaPagoUtc = pago.FechaPagoUtc,
            CorrelationId = sourceEvent.CorrelationId,
            CausationId = sourceEvent.EventId
        };

        db.OutboxMessages.Add(new OutboxMessageEntity
        {
            EventId = evt.EventId,
            EventType = evt.EventType,
            EventVersion = evt.EventVersion,
            RoutingKey = "facturacion.pago.registrado.v1",
            Payload = JsonSerializer.Serialize(evt, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            CorrelationId = evt.CorrelationId,
            CausationId = evt.CausationId,
            Source = evt.Source,
            IdempotencyKey = idempotencyKey,
            OccurredOnUtc = evt.OccurredOnUtc,
            CreatedOnUtc = DateTime.UtcNow,
            Status = "PEN"
        });
    }

    private static int ToPositiveLogicalId(Guid guid)
    {
        if (guid == Guid.Empty)
            return 1;

        var value = BitConverter.ToInt32(guid.ToByteArray(), 0) & int.MaxValue;
        return value == 0 ? 1 : value;
    }

    private static void AddFacturaGeneradaOutbox(FacturacionDbContext db, FacturaEntity factura, ReservaConfirmadaIntegrationEvent sourceEvent)
    {
        if (db.OutboxMessages.Any(message =>
            message.EventType == "facturacion.factura.generada" &&
            message.IdempotencyKey == $"factura-generada:{factura.GuidFactura:N}"))
        {
            return;
        }

        var evt = new FacturaGeneradaIntegrationEvent
        {
            FacturaGuid = factura.GuidFactura,
            ReservaGuid = sourceEvent.ReservaGuid,
            NumeroFactura = factura.NumeroFactura,
            TipoFactura = factura.TipoFactura,
            Subtotal = factura.Subtotal,
            ValorIva = factura.ValorIva,
            Total = factura.Total,
            Saldo = factura.SaldoPendiente,
            EstadoFactura = factura.Estado,
            CorrelationId = sourceEvent.CorrelationId,
            CausationId = sourceEvent.EventId
        };

        db.OutboxMessages.Add(new OutboxMessageEntity
        {
            EventId = evt.EventId,
            EventType = evt.EventType,
            EventVersion = evt.EventVersion,
            RoutingKey = "facturacion.factura.generada.v1",
            Payload = JsonSerializer.Serialize(evt, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            CorrelationId = evt.CorrelationId,
            CausationId = evt.CausationId,
            Source = evt.Source,
            IdempotencyKey = $"factura-generada:{factura.GuidFactura:N}",
            OccurredOnUtc = evt.OccurredOnUtc,
            CreatedOnUtc = DateTime.UtcNow,
            Status = "PEN"
        });
    }

    private static bool RegisterInbox(FacturacionDbContext db, Guid eventId, string eventType, string eventVersion, string source, Guid correlationId)
    {
        if (db.InboxMessages.Any(m => m.EventId == eventId))
            return false;

        db.InboxMessages.Add(new InboxMessageEntity
        {
            EventId = eventId,
            EventType = eventType,
            EventVersion = eventVersion,
            Source = source,
            CorrelationId = correlationId
        });
        db.SaveChanges();
        return true;
    }

    private static void MarkInboxProcessed(FacturacionDbContext db, Guid eventId)
    {
        var inbox = db.InboxMessages.First(m => m.EventId == eventId);
        inbox.Status = "PRO";
        inbox.ProcessedOnUtc = DateTime.UtcNow;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
