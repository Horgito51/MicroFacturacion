using Facturacion.DataAccess.Context;
using Facturacion.DataAccess.Entities.Eventing;
using Facturacion.DataManagement.Eventing.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.DataManagement.Eventing.Services;

public sealed class OutboxMessageService : IOutboxMessageService
{
    private readonly FacturacionDbContext _context;

    public OutboxMessageService(FacturacionDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessageEntity message, CancellationToken cancellationToken = default)
    {
        message.CreatedOnUtc = DateTime.UtcNow;
        message.Status = string.IsNullOrWhiteSpace(message.Status) ? "PEN" : message.Status;
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessageEntity>> GetPendingAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 500);

        return await _context.OutboxMessages
            .Where(message => message.Status == "PEN")
            .OrderBy(message => message.CreatedOnUtc)
            .Take(normalizedTake)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkPublishedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FirstAsync(message => message.EventId == eventId, cancellationToken);
        message.Status = "PUB";
        message.PublishedOnUtc = DateTime.UtcNow;
        message.LastError = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FirstAsync(message => message.EventId == eventId, cancellationToken);
        message.Status = "PEN";
        message.PublishAttempts++;
        message.LastError = string.IsNullOrWhiteSpace(error) ? "Error no especificado." : error[..Math.Min(error.Length, 2000)];
        await _context.SaveChangesAsync(cancellationToken);
    }
}
