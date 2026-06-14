using Facturacion.DataAccess.Entities.Eventing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Facturacion.DataAccess.Configurations.Eventing;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable("INBOX_MESSAGES", "eventing");
        builder.HasKey(message => message.IdInboxMessage);

        builder.Property(message => message.IdInboxMessage).HasColumnName("id_inbox_message").ValueGeneratedOnAdd();
        builder.Property(message => message.EventId).HasColumnName("event_id");
        builder.Property(message => message.EventType).HasColumnName("event_type").HasMaxLength(150);
        builder.Property(message => message.EventVersion).HasColumnName("event_version").HasMaxLength(20);
        builder.Property(message => message.Source).HasColumnName("source").HasMaxLength(80);
        builder.Property(message => message.CorrelationId).HasColumnName("correlation_id");
        builder.Property(message => message.ReceivedOnUtc).HasColumnName("received_on_utc");
        builder.Property(message => message.ProcessedOnUtc).HasColumnName("processed_on_utc");
        builder.Property(message => message.ProcessAttempts).HasColumnName("process_attempts");
        builder.Property(message => message.Status).HasColumnName("status").HasMaxLength(10);
        builder.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(2000);

        builder.HasIndex(message => message.EventId).IsUnique();
        builder.HasIndex(message => new { message.Status, message.ReceivedOnUtc });
    }
}
