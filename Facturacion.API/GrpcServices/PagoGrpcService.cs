using Facturacion.Business.DTOs.Facturacion;
using Facturacion.Business.Interfaces.Facturacion;
using Facturacion.Contracts.Grpc.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Facturacion.API.GrpcServices;

public class PagoGrpcService : PagoGrpc.PagoGrpcBase
{
    private readonly IPagoService _pagoService;

    public PagoGrpcService(IPagoService pagoService)
    {
        _pagoService = pagoService;
    }

    public override async Task<Pago> GetPagoById(IdRequest request, ServerCallContext context)
    {
        try
        {
            return ToGrpc(await _pagoService.GetByIdAsync(request.Id, context.CancellationToken));
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<Pago> GetPagoByGuid(GuidRequest request, ServerCallContext context)
    {
        try
        {
            return ToGrpc(await _pagoService.GetByGuidAsync(Guid.Parse(request.Guid), context.CancellationToken));
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<PagoPage> ListPagos(PageRequest request, ServerCallContext context)
    {
        try
        {
            var page = await _pagoService.GetAllAsync(
                request.PageNumber <= 0 ? 1 : request.PageNumber,
                request.PageSize <= 0 ? 50 : request.PageSize,
                context.CancellationToken);

            var response = new PagoPage
            {
                TotalCount = page.TotalCount,
                PageNumber = page.PageNumber,
                PageSize = page.PageSize
            };
            response.Items.AddRange(page.Items.Select(ToGrpc));
            return response;
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<PagoPage> ListPagosByFactura(PagosByFacturaRequest request, ServerCallContext context)
    {
        try
        {
            var page = await _pagoService.GetByFacturaAsync(
                request.IdFactura,
                request.PageNumber <= 0 ? 1 : request.PageNumber,
                request.PageSize <= 0 ? 50 : request.PageSize,
                context.CancellationToken);

            var response = new PagoPage
            {
                TotalCount = page.TotalCount,
                PageNumber = page.PageNumber,
                PageSize = page.PageSize
            };
            response.Items.AddRange(page.Items.Select(ToGrpc));
            return response;
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<Pago> CreatePago(PagoCreateRequest request, ServerCallContext context)
    {
        try
        {
            var pago = await _pagoService.CreateAsync(new PagoDTO
            {
                IdFactura = request.IdFactura,
                IdReserva = request.IdReserva,
                Monto = ParseDecimal(request.Monto),
                MetodoPago = request.MetodoPago,
                EsPagoElectronico = request.EsPagoElectronico,
                ProveedorPasarela = request.ProveedorPasarela,
                TransaccionExterna = request.TransaccionExterna,
                CodigoAutorizacion = request.CodigoAutorizacion,
                Referencia = request.Referencia,
                EstadoPago = "APR",
                FechaPagoUtc = DateTime.UtcNow,
                Moneda = string.IsNullOrWhiteSpace(request.Moneda) ? "USD" : request.Moneda,
                TipoCambio = ParseDecimal(request.TipoCambio, 1m),
                CreadoPorUsuario = "Middleware.HotelJJ",
                ServicioOrigen = "pagos-grpc"
            }, context.CancellationToken);

            return ToGrpc(pago);
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<Empty> UpdatePagoEstado(PagoEstadoRequest request, ServerCallContext context)
    {
        try
        {
            await _pagoService.UpdateEstadoAsync(
                request.IdPago,
                request.NuevoEstado,
                request.Usuario,
                context.CancellationToken);

            return new Empty();
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<PagoSimulado> SimularPago(PagoSimularRequest request, ServerCallContext context)
    {
        try
        {
            var pago = await _pagoService.SimularPagoAsync(
                request.IdReserva,
                ParseDecimal(request.Monto),
                request.Usuario,
                request.TokenPago,
                request.Referencia,
                context.CancellationToken);

            return new PagoSimulado
            {
                IdReserva = pago.IdReserva,
                CodigoReserva = pago.CodigoReserva ?? string.Empty,
                Monto = FormatDecimal(pago.Monto),
                EstadoPago = pago.EstadoPago ?? string.Empty,
                EstadoReserva = pago.EstadoReserva ?? string.Empty,
                TransaccionExterna = pago.TransaccionExterna ?? string.Empty,
                CodigoAutorizacion = pago.CodigoAutorizacion ?? string.Empty,
                Mensaje = pago.Mensaje ?? string.Empty,
                FechaPagoUtc = Timestamp.FromDateTime(pago.FechaPagoUtc.ToUniversalTime())
            };
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    public override async Task<TotalPagadoResponse> GetTotalPagadoPorFactura(IdRequest request, ServerCallContext context)
    {
        try
        {
            var total = await _pagoService.GetTotalPagadoPorFacturaAsync(request.Id, context.CancellationToken);
            return new TotalPagadoResponse
            {
                IdFactura = request.Id,
                TotalPagado = FormatDecimal(total)
            };
        }
        catch (Exception ex)
        {
            throw GrpcExceptionMapper.Map(ex);
        }
    }

    private static Pago ToGrpc(PagoDTO dto)
    {
        return new Pago
        {
            IdPago = dto.IdPago,
            PagoGuid = dto.PagoGuid.ToString(),
            IdFactura = dto.IdFactura,
            IdReserva = dto.IdReserva,
            Monto = FormatDecimal(dto.Monto),
            MetodoPago = dto.MetodoPago ?? string.Empty,
            EsPagoElectronico = dto.EsPagoElectronico,
            ProveedorPasarela = dto.ProveedorPasarela ?? string.Empty,
            TransaccionExterna = dto.TransaccionExterna ?? string.Empty,
            CodigoAutorizacion = dto.CodigoAutorizacion ?? string.Empty,
            Referencia = dto.Referencia ?? string.Empty,
            EstadoPago = dto.EstadoPago ?? string.Empty,
            FechaPagoUtc = Timestamp.FromDateTime(dto.FechaPagoUtc.ToUniversalTime()),
            Moneda = dto.Moneda ?? string.Empty,
            TipoCambio = FormatDecimal(dto.TipoCambio),
            RespuestaPasarela = dto.RespuestaPasarela ?? string.Empty,
            RowVersion = Google.Protobuf.ByteString.CopyFrom(dto.RowVersion ?? Array.Empty<byte>())
        };
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static decimal ParseDecimal(string value, decimal defaultValue = 0m)
    {
        return decimal.TryParse(
            value,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : defaultValue;
    }
}
