using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Facturacion.DataAccess.Entities.Facturacion;

namespace Facturacion.DataAccess.Repositories.Interfaces.Facturacion
{
    public interface IFacturaRepository
    {
        // CRUD b�sico
        Task<FacturaEntity?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<FacturaEntity?> GetByGuidAsync(Guid guid, CancellationToken ct = default);
        Task<IEnumerable<FacturaEntity>> GetAllAsync(CancellationToken ct = default);
        Task<FacturaEntity> AddAsync(FacturaEntity entity, CancellationToken ct = default);
        Task UpdateAsync(FacturaEntity entity, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);

        // Operaciones de escritura
        Task UpdateSaldoPendienteAsync(int idFactura, decimal nuevoSaldo, CancellationToken ct = default);
        Task AnularAsync(int idFactura, string motivo, string usuario, CancellationToken ct = default);
        Task<bool> EstaPagadaAsync(int idFactura, CancellationToken ct = default);

        // M�todos para generar facturas (ejecutan SP)
        Task<int> GenerarFacturaReservaAsync(int idReserva, string usuario, CancellationToken ct = default);
        Task<int> GenerarFacturaFinalAsync(int idReserva, string usuario, CancellationToken ct = default);
    }
}