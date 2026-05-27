using Facturacion.Business.Interfaces.Facturacion;
using Facturacion.Contracts.Grpc.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Facturacion.API.GrpcServices;

public class FacturacionGrpcService : Facturacion.Contracts.Grpc.V1.FacturacionGrpc.FacturacionGrpcBase
{
    private readonly IFacturaService _facturaService;

    public FacturacionGrpcService(IFacturaService facturaService)
    {
        _facturaService = facturaService;
    }

    public override async Task<Factura> GetFacturaById(IdRequest request, ServerCallContext context)
    {
        try
        {
            return ToGrpc(await _facturaService.GetByIdAsync(request.Id, context.CancellationToken));
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<Factura> GetFacturaByGuid(GuidRequest request, ServerCallContext context)
    {
        try
        {
            return ToGrpc(await _facturaService.GetByGuidAsync(Guid.Parse(request.Guid), context.CancellationToken));
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<GenerarFacturaResponse> GenerarFacturaReserva(GenerarFacturaRequest request, ServerCallContext context)
    {
        try
        {
            var idFactura = await _facturaService.GenerarFacturaReservaAsync(request.IdReserva, request.Usuario, context.CancellationToken);
            return new GenerarFacturaResponse { IdFactura = idFactura };
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<GenerarFacturaResponse> GenerarFacturaFinal(GenerarFacturaRequest request, ServerCallContext context)
    {
        try
        {
            var idFactura = await _facturaService.GenerarFacturaFinalAsync(request.IdReserva, request.Usuario, context.CancellationToken);
            return new GenerarFacturaResponse { IdFactura = idFactura };
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<Empty> AnularFactura(AnularFacturaRequest request, ServerCallContext context)
    {
        try
        {
            await _facturaService.AnularAsync(request.IdFactura, request.Motivo, request.Usuario, context.CancellationToken);
            return new Empty();
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<SaldoPendienteResponse> GetSaldoPendiente(IdRequest request, ServerCallContext context)
    {
        try
        {
            var factura = await _facturaService.GetByIdAsync(request.Id, context.CancellationToken);
            return new SaldoPendienteResponse
            {
                IdFactura = factura.IdFactura,
                SaldoPendiente = factura.SaldoPendiente.ToString("0.##")
            };
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    private static Factura ToGrpc(Facturacion.Business.DTOs.Facturacion.FacturaDTO dto)
    {
        var grpc = new Factura
        {
            IdFactura = dto.IdFactura,
            FacturaGuid = dto.GuidFactura.ToString(),
            IdCliente = dto.IdCliente,
            IdReserva = dto.IdReserva,
            IdSucursal = dto.IdSucursal,
            NumeroFactura = dto.NumeroFactura ?? string.Empty,
            TipoFactura = dto.TipoFactura ?? string.Empty,
            FechaEmision = Timestamp.FromDateTime(dto.FechaEmision.ToUniversalTime()),
            Subtotal = dto.Subtotal.ToString("0.##"),
            ValorIva = dto.ValorIva.ToString("0.##"),
            DescuentoTotal = dto.DescuentoTotal.ToString("0.##"),
            Total = dto.Total.ToString("0.##"),
            SaldoPendiente = dto.SaldoPendiente.ToString("0.##"),
            Moneda = dto.Moneda ?? string.Empty,
            ObservacionesFactura = dto.ObservacionesFactura ?? string.Empty,
            OrigenCanalFactura = dto.OrigenCanalFactura ?? string.Empty,
            Estado = dto.Estado ?? string.Empty,
            RowVersion = Google.Protobuf.ByteString.CopyFrom(dto.RowVersion ?? Array.Empty<byte>())
        };

        grpc.Detalles.AddRange((dto.Detalles ?? []).Select(ToGrpc));
        return grpc;
    }

    private static FacturaDetalle ToGrpc(Facturacion.Business.DTOs.Facturacion.FacturaDetalleDTO dto)
    {
        return new FacturaDetalle
        {
            IdFacturaDetalle = dto.IdFacturaDetalle,
            FacturaDetalleGuid = dto.FacturaDetalleGuid.ToString(),
            IdFactura = dto.IdFactura,
            TipoItem = dto.TipoItem ?? string.Empty,
            ReferenciaTipo = dto.ReferenciaTipo ?? string.Empty,
            ReferenciaId = dto.ReferenciaId,
            DescripcionItem = dto.DescripcionItem ?? string.Empty,
            Cantidad = dto.Cantidad,
            PrecioUnitario = dto.PrecioUnitario.ToString("0.##"),
            SubtotalLinea = dto.SubtotalLinea.ToString("0.##"),
            ValorIvaLinea = dto.ValorIvaLinea.ToString("0.##"),
            DescuentoLinea = dto.DescuentoLinea.ToString("0.##"),
            TotalLinea = dto.TotalLinea.ToString("0.##"),
            RowVersion = Google.Protobuf.ByteString.CopyFrom(dto.RowVersion ?? Array.Empty<byte>())
        };
    }
}
