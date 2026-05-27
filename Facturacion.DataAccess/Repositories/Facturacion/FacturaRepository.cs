using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Facturacion.DataAccess.Context;
using Facturacion.DataAccess.Entities.Facturacion;
using Facturacion.DataAccess.Repositories.Interfaces.Facturacion;

namespace Facturacion.DataAccess.Repositories.Facturacion
{
    public class FacturaRepository : RepositoryBase<FacturaEntity>, IFacturaRepository
    {
        public FacturaRepository(FacturacionDbContext context) : base(context) { }

        public async Task<FacturaEntity?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _dbSet
                .Include(f => f.FacturaDetalles)
                .FirstOrDefaultAsync(f => f.IdFactura == id, ct);

        public async Task<FacturaEntity?> GetByGuidAsync(Guid guid, CancellationToken ct = default)
            => await _dbSet
                .Include(f => f.FacturaDetalles)
                .FirstOrDefaultAsync(f => f.GuidFactura == guid, ct);

        public async Task<IEnumerable<FacturaEntity>> GetAllAsync(CancellationToken ct = default)
            => await base.GetAllAsync(ct);

        public async Task<FacturaEntity> AddAsync(FacturaEntity entity, CancellationToken ct = default)
            => await base.AddAsync(entity, ct);

        public async Task UpdateAsync(FacturaEntity entity, CancellationToken ct = default)
            => await base.UpdateAsync(entity, ct);

        public async Task DeleteAsync(int id, CancellationToken ct = default)
            => await base.DeleteAsync(id, ct);

        public async Task UpdateSaldoPendienteAsync(int idFactura, decimal nuevoSaldo, CancellationToken ct = default)
        {
            var factura = await GetByIdAsync(idFactura, ct);
            if (factura != null)
            {
                factura.SaldoPendiente = nuevoSaldo;
                if (nuevoSaldo == 0) factura.Estado = "PAG";
                await UpdateAsync(factura, ct);
            }
        }

        public async Task AnularAsync(int idFactura, string motivo, string usuario, CancellationToken ct = default)
        {
            var factura = await GetByIdAsync(idFactura, ct);
            if (factura != null)
            {
                factura.Estado = "ANU";
                factura.MotivoInhabilitacion = motivo;
                factura.ModificadoPorUsuario = usuario;
                factura.FechaModificacionUtc = DateTime.UtcNow;
                await UpdateAsync(factura, ct);
            }
        }

        public async Task<bool> EstaPagadaAsync(int idFactura, CancellationToken ct = default)
        {
            var factura = await GetByIdAsync(idFactura, ct);
            return factura != null && factura.Estado == "PAG";
        }

        public async Task<int> GenerarFacturaReservaAsync(int idReserva, string usuario, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Facturacion no puede consultar Reservas directamente. Genere la factura con datos recibidos por integracion o SP_GENERAR_FACTURA_RESERVA.");
        }

        public async Task<int> GenerarFacturaFinalAsync(int idReserva, string usuario, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Facturacion no puede consultar Reservas directamente. Genere la factura final con cargos recibidos por integracion o SP_GENERAR_FACTURA_FINAL.");
        }
    }
}
