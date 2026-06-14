using Facturacion.DataAccess.Entities.Eventing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Facturacion.DataAccess.Configurations.Eventing;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("OUTBOX_MESSAGES", "eventing");
        builder.HasKey(message => message.IdOutboxMessage);

        builder.Property(message => message.IdOutboxMessage).HasColumnName("id_outbox_message").ValueGeneratedOnAdd();
        builder.Property(message => message.EventId).HasColumnName("event_id");
        builder.Property(message => message.EventType).HasColumnName("event_type").HasMaxLength(150);
        builder.Property(message => message.EventVersion).HasColumnName("event_version").HasMaxLength(20);
        builder.Property(message => message.RoutingKey).HasColumnName("routing_key").HasMaxLength(200);
        builder.Property(message => message.Payload).HasColumnName("payload");
        builder.Property(message => message.CorrelationId).HasColumnName("correlation_id");
        builder.Property(message => message.CausationId).HasColumnName("causation_id");
        builder.Property(message => message.Source).HasColumnName("source").HasMaxLength(80);
        builder.Property(message => message.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(120);
        builder.Property(message => message.OccurredOnUtc).HasColumnName("occurred_on_utc");
        builder.Property(message => message.CreatedOnUtc).HasColumnName("created_on_utc");
        builder.Property(message => message.PublishedOnUtc).HasColumnName("published_on_utc");
        builder.Property(message => message.PublishAttempts).HasColumnName("publish_attempts");
        builder.Property(message => message.Status).HasColumnName("status").HasMaxLength(10);
        builder.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(2000);

        builder.HasIndex(message => message.EventId).IsUnique();
        builder.HasIndex(message => new { message.Status, message.CreatedOnUtc });
    }
}
