using Facturacion.DataAccess.Entities.Facturacion;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.DataAccess.Context
{
    public class FacturacionDbContext : DbContext
    {
        public FacturacionDbContext(DbContextOptions<FacturacionDbContext> options) : base(options)
        {
        }

        public DbSet<FacturaEntity> Facturas => Set<FacturaEntity>();
        public DbSet<FacturaDetalleEntity> FacturaDetalles => Set<FacturaDetalleEntity>();
        public DbSet<PagoEntity> Pagos => Set<PagoEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FacturacionDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
