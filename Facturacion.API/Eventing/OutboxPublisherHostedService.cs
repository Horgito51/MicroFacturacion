using Facturacion.DataManagement.Eventing.Interfaces;

namespace Facturacion.API.Eventing;

public sealed class OutboxPublisherHostedService : BackgroundService
{
    private readonly ILogger<OutboxPublisherHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OutboxPublisherHostedService(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IOutboxMessageService>();
                var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
                var pending = await outbox.GetPendingAsync(25, stoppingToken);

                foreach (var message in pending)
                {
                    try
                    {
                        await eventBus.PublishJsonAsync(
                            message.RoutingKey,
                            message.Payload,
                            message.EventId,
                            message.CorrelationId,
                            message.EventType,
                            stoppingToken);

                        await outbox.MarkPublishedAsync(message.EventId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo publicar Outbox de Facturacion EventId={EventId}", message.EventId);
                        await outbox.MarkFailedAsync(message.EventId, ex.Message, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OutboxPublisherHostedService de Facturacion.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
