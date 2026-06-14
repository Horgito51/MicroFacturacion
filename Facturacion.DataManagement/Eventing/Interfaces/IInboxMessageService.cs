using Facturacion.DataAccess.Entities.Eventing;

namespace Facturacion.DataManagement.Eventing.Interfaces;

public interface IInboxMessageService
{
    Task<bool> TryRegisterReceivedAsync(InboxMessageEntity message, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken = default);
}
