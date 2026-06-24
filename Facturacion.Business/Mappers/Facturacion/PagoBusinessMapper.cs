using System.Collections.Generic;
using System.Linq;
using Facturacion.Business.DTOs.Facturacion;
using Facturacion.DataManagement.Facturacion.Models;

namespace Facturacion.Business.Mappers.Facturacion
{
    public static class PagoBusinessMapper
    {
        public static PagoDTO ToDto(this PagoDataModel model)
        {
            if (model == null) return null;

            return new PagoDTO
            {
                IdPago = model.IdPago,
                PagoGuid = model.PagoGuid,
                IdFactura = model.IdFactura,
                IdReserva = model.IdReserva,
                ReservaGuid = model.ReservaGuid,
                Monto = model.Monto,
                MetodoPago = model.MetodoPago,
                EsPagoElectronico = model.EsPagoElectronico,
                ProveedorPasarela = model.ProveedorPasarela,
                TransaccionExterna = model.TransaccionExterna,
                CodigoAutorizacion = model.CodigoAutorizacion,
                Referencia = model.Referencia,
                EstadoPago = model.EstadoPago,
                FechaPagoUtc = model.FechaPagoUtc,
                Moneda = model.Moneda,
                TipoCambio = model.TipoCambio,
                RespuestaPasarela = model.RespuestaPasarela,
                CreadoPorUsuario = model.CreadoPorUsuario,
                FechaRegistroUtc = model.FechaRegistroUtc,
                ModificadoPorUsuario = model.ModificadoPorUsuario,
                FechaModificacionUtc = model.FechaModificacionUtc,
                ModificacionIp = model.ModificacionIp,
                ServicioOrigen = model.ServicioOrigen,
                RowVersion = model.RowVersion
            };
        }

        public static PagoDataModel ToDataModel(this PagoDTO dto)
        {
            if (dto == null) return null;

            return new PagoDataModel
            {
                IdPago = dto.IdPago,
                PagoGuid = dto.PagoGuid,
                IdFactura = dto.IdFactura,
                IdReserva = dto.IdReserva,
                ReservaGuid = dto.ReservaGuid,
                Monto = dto.Monto,
                MetodoPago = dto.MetodoPago,
                EsPagoElectronico = dto.EsPagoElectronico,
                ProveedorPasarela = dto.ProveedorPasarela,
                TransaccionExterna = dto.TransaccionExterna,
                CodigoAutorizacion = dto.CodigoAutorizacion,
                Referencia = dto.Referencia,
                EstadoPago = dto.EstadoPago,
                FechaPagoUtc = dto.FechaPagoUtc,
                Moneda = dto.Moneda,
                TipoCambio = dto.TipoCambio,
                RespuestaPasarela = dto.RespuestaPasarela,
                CreadoPorUsuario = dto.CreadoPorUsuario,
                FechaRegistroUtc = dto.FechaRegistroUtc,
                ModificadoPorUsuario = dto.ModificadoPorUsuario,
                FechaModificacionUtc = dto.FechaModificacionUtc,
                ModificacionIp = dto.ModificacionIp,
                ServicioOrigen = dto.ServicioOrigen,
                RowVersion = dto.RowVersion
            };
        }

        public static List<PagoDTO> ToDtoList(this IEnumerable<PagoDataModel> models)
            => models?.Select(m => m.ToDto()).ToList() ?? new();

        public static List<PagoDataModel> ToDataModelList(this IEnumerable<PagoDTO> dtos)
            => dtos?.Select(d => d.ToDataModel()).ToList() ?? new();
    }
}
