using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Facturacion.API.Models.Requests.Public;
using Facturacion.API.Models.Responses.Public;
using Facturacion.Business.Exceptions;
using Facturacion.Business.Interfaces.Facturacion;

namespace Facturacion.API.Controllers.V1.Booking
{
    [ApiController]
    [AllowAnonymous]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/public/pagos")]
    public class PagosPublicController : ControllerBase
    {
        private readonly IPagoService _pagoService;
        private readonly ILogger<PagosPublicController> _logger;

        public PagosPublicController(IPagoService pagoService, ILogger<PagosPublicController> logger)
        {
            _pagoService = pagoService;
            _logger = logger;
        }

        [HttpPost("simular")]
        [AllowAnonymous]
        public async Task<ActionResult<PagoSimuladoPublicDto>> Simular([FromBody] PublicPagoSimularRequest request)
        {
            try
            {
                request.ValidateNoIds();

                if (request.ReservaGuid == Guid.Empty)
                    throw new ValidationException("PAG-PUB-001", "reservaGuid es obligatorio.");

                throw new NotSupportedException("El pago publico por reservaGuid requiere una integracion HTTP/gRPC con Reservas.");
            }
            catch (Exception ex)
            {
                // #region agent log
                _logger.LogWarning(
                    ex,
                    "Facturacion public payment diagnostic {HypothesisId} at {Location}: {Message}. Data: {@Data}",
                    "H29",
                    "Facturacion.API/Controllers/V1/Booking/PagosPublicController.cs:Simular:catch",
                    "Unhandled exception in public payment endpoint",
                    new { HttpContext.TraceIdentifier, ex.Message, Type = ex.GetType().FullName });
                // #endregion
                throw;
            }
        }
    }
}
