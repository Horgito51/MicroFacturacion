using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Facturacion.DataAccess.Common.Pagination;
using Facturacion.Business.DTOs.Facturacion;
using Facturacion.Business.Exceptions;
using Facturacion.Business.Interfaces.Facturacion;
using Facturacion.Business.Mappers.Facturacion;
using Facturacion.Business.Validators.Facturacion;
using Facturacion.DataManagement.Facturacion.Interfaces;
using Facturacion.DataManagement.UnitOfWork;

namespace Facturacion.Business.Services.Facturacion
{
    public class PagoService : IPagoService
    {
        private readonly IPagoDataService _pagoDataService;
        private readonly IFacturaService _facturaService;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IUnitOfWork _unitOfWork;

        public PagoService(
            IPagoDataService pagoDataService,
            IFacturaService facturaService,
            IPaymentGateway paymentGateway,
            IUnitOfWork unitOfWork)
        {
            _pagoDataService = pagoDataService;
            _facturaService = facturaService;
            _paymentGateway = paymentGateway;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagoDTO> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var dataModel = await _pagoDataService.GetByIdAsync(id, ct);
            if (dataModel == null)
                throw new NotFoundException("PAG-001", $"No se encontró el pago con ID {id}.");
            return dataModel.ToDto();
        }

        public async Task<PagoDTO> GetByGuidAsync(Guid guid, CancellationToken ct = default)
        {
            var dataModel = await _pagoDataService.GetByGuidAsync(guid, ct);
            if (dataModel == null)
                throw new NotFoundException("PAG-002", $"No se encontró el pago con GUID {guid}.");
            return dataModel.ToDto();
        }

        public async Task<PagedResult<PagoDTO>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var pagedData = await _pagoDataService.GetAllAsync(pageNumber, pageSize, ct);
            return new PagedResult<PagoDTO>
            {
                Items = pagedData.Items.ToDtoList(),
                TotalCount = pagedData.TotalCount,
                PageNumber = pagedData.PageNumber,
                PageSize = pagedData.PageSize
            };
        }

        public async Task<PagedResult<PagoDTO>> GetByFacturaAsync(int idFactura, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            if (idFactura <= 0)
                throw new ValidationException("PAG-007", "El idFactura es obligatorio para consultar pagos por factura.");

            var pagedData = await _pagoDataService.GetByFacturaAsync(idFactura, pageNumber, pageSize, ct);
            return new PagedResult<PagoDTO>
            {
                Items = pagedData.Items.ToDtoList(),
                TotalCount = pagedData.TotalCount,
                PageNumber = pagedData.PageNumber,
                PageSize = pagedData.PageSize
            };
        }

        public async Task<PagoDTO> CreateAsync(PagoDTO pagoDto, CancellationToken ct = default)
        {
            pagoDto.EstadoPago = string.IsNullOrWhiteSpace(pagoDto.EstadoPago) ? "APR" : pagoDto.EstadoPago;
            pagoDto.FechaPagoUtc = pagoDto.FechaPagoUtc == default ? DateTime.UtcNow : pagoDto.FechaPagoUtc;
            pagoDto.Moneda = string.IsNullOrWhiteSpace(pagoDto.Moneda) ? "USD" : pagoDto.Moneda;
            pagoDto.TipoCambio = pagoDto.TipoCambio <= 0 ? 1 : pagoDto.TipoCambio;
            PagoValidator.Validate(pagoDto);
            var dataModel = pagoDto.ToDataModel();
            var created = await _pagoDataService.AddAsync(dataModel, ct);

            if (created.EstadoPago == "APR")
            {
                var factura = await _facturaService.GetByIdAsync(created.IdFactura, ct);
                var nuevoSaldo = Math.Max(0, factura.SaldoPendiente - created.Monto);
                await _facturaService.UpdateSaldoPendienteAsync(created.IdFactura, nuevoSaldo, ct);
            }

            return created.ToDto();
        }

        public async Task UpdateAsync(PagoDTO pagoDto, CancellationToken ct = default)
        {
            var existing = await _pagoDataService.GetByIdAsync(pagoDto.IdPago, ct);
            if (existing == null)
                throw new NotFoundException("PAG-003", $"No se encontró el pago con ID {pagoDto.IdPago}.");
            var dataModel = pagoDto.ToDataModel();
            await _pagoDataService.UpdateAsync(dataModel, ct);
        }

        public async Task UpdateEstadoAsync(int idPago, string nuevoEstado, string usuario, CancellationToken ct = default)
        {
            var existing = await _pagoDataService.GetByIdAsync(idPago, ct);
            if (existing == null)
                throw new NotFoundException("PAG-004", $"No se encontró el pago con ID {idPago}.");
            await _pagoDataService.UpdateEstadoAsync(idPago, nuevoEstado, usuario, ct);
        }

        public async Task<decimal> GetTotalPagadoPorFacturaAsync(int idFactura, CancellationToken ct = default)
        {
            return await _pagoDataService.GetTotalPagadoPorFacturaAsync(idFactura, ct);
        }

        public async Task<PagoSimuladoDTO> SimularPagoAsync(int idReserva, decimal? monto, string usuario, string? tokenPago = null, string? referencia = null, CancellationToken ct = default)
        {
            await Task.CompletedTask;
            throw new NotSupportedException("La simulacion de pago por reserva requiere integracion con Reservas; Facturacion no referencia otros microservicios directamente.");
        }
    }
}
