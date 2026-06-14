using Facturacion.DataAccess.Context;
using Facturacion.DataAccess.Entities.Eventing;
using Facturacion.DataManagement.Eventing.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.DataManagement.Eventing.Services;

public sealed class InboxMessageService : IInboxMessageService
{
    private readonly FacturacionDbContext _context;

    public InboxMessageService(FacturacionDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryRegisterReceivedAsync(InboxMessageEntity message, CancellationToken cancellationToken = default)
    {
        var exists = await _context.InboxMessages.AnyAsync(existing => existing.EventId == message.EventId, cancellationToken);
        if (exists)
        {
            return false;
        }

        message.ReceivedOnUtc = DateTime.UtcNow;
        message.Status = string.IsNullOrWhiteSpace(message.Status) ? "REC" : message.Status;
        await _context.InboxMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var message = await _context.InboxMessages.FirstAsync(message => message.EventId == eventId, cancellationToken);
        message.Status = "PRO";
        message.ProcessedOnUtc = DateTime.UtcNow;
        message.LastError = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.InboxMessages.FirstAsync(message => message.EventId == eventId, cancellationToken);
        message.Status = "ERR";
        message.ProcessAttempts++;
        message.LastError = string.IsNullOrWhiteSpace(error) ? "Error no especificado." : error[..Math.Min(error.Length, 2000)];
        await _context.SaveChangesAsync(cancellationToken);
    }
}
