using Facturacion.DataAccess.Entities.Eventing;

namespace Facturacion.DataManagement.Eventing.Interfaces;

public interface IOutboxMessageService
{
    Task AddAsync(OutboxMessageEntity message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessageEntity>> GetPendingAsync(int take = 50, CancellationToken cancellationToken = default);
    Task MarkPublishedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken = default);
}
