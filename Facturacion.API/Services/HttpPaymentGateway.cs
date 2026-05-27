using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Facturacion.API.Models.Settings;
using Facturacion.Business.Exceptions;
using Facturacion.Business.Interfaces.Facturacion;

namespace Facturacion.API.Services
{
    public class HttpPaymentGateway : IPaymentGateway
    {
        private readonly HttpClient _httpClient;
        private readonly PaymentGatewaySettings _settings;
        private readonly ILogger<HttpPaymentGateway> _logger;

        private void WriteDebugLog(string hypothesisId, string location, string message, object data)
        {
            _logger.LogWarning(
                "Facturacion payment gateway diagnostic {HypothesisId} at {Location}: {Message}. Data: {@Data}",
                hypothesisId,
                location,
                message,
                data);
        }

        public HttpPaymentGateway(HttpClient httpClient, IOptions<PaymentGatewaySettings> options, ILogger<HttpPaymentGateway> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<PaymentGatewayResult> ProcesarPagoAsync(PaymentGatewayRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.TokenPago))
                throw new ValidationException("PAG-GW-002", "El token de pago emitido por la pasarela es obligatorio.");

            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
                return CrearPagoSimulado(request);

            Uri url;
            try
            {
                var baseUrl = (_settings.BaseUrl ?? string.Empty).TrimEnd('/') + "/";
                var chargePath = (_settings.ChargePath ?? string.Empty).TrimStart('/');
                url = new Uri(new Uri(baseUrl), chargePath);
            }
            catch (Exception ex)
            {
                // #region agent log
                WriteDebugLog(
                    "H28",
                    "Facturacion.API/Services/HttpPaymentGateway.cs:ProcesarPagoAsync:invalidGatewayConfig",
                    "Invalid gateway URL configuration",
                    new { _settings.BaseUrl, _settings.ChargePath, ex.Message });
                // #endregion
                return new PaymentGatewayResult
                {
                    Aprobado = false,
                    Estado = "REC",
                    Mensaje = "La configuración de la pasarela de pago no es válida.",
                    RespuestaRaw = ex.Message ?? string.Empty
                };
            }
            using var message = new HttpRequestMessage(HttpMethod.Post, url);

            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var body = new
            {
                amount = request.Monto,
                currency = request.Moneda,
                paymentToken = request.TokenPago,
                reference = request.Referencia,
                reservationId = request.IdReserva,
                invoiceId = request.IdFactura,
                reservationCode = request.CodigoReserva
            };

            message.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            string raw;
            try
            {
                response = await _httpClient.SendAsync(message, ct);
                raw = await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                // #region agent log
                WriteDebugLog(
                    "H25",
                    "Facturacion.API/Services/HttpPaymentGateway.cs:ProcesarPagoAsync:sendError",
                    "Gateway HTTP call failed",
                    new { _settings.BaseUrl, _settings.ChargePath, ex.Message });
                // #endregion
                return new PaymentGatewayResult
                {
                    Aprobado = false,
                    Estado = "REC",
                    Mensaje = "No se pudo conectar con la pasarela de pago. Intenta nuevamente en unos minutos.",
                    RespuestaRaw = ex.Message ?? string.Empty
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                // #region agent log
                WriteDebugLog(
                    "H26",
                    "Facturacion.API/Services/HttpPaymentGateway.cs:ProcesarPagoAsync:nonSuccessStatus",
                    "Gateway HTTP non-success status",
                    new { Status = (int)response.StatusCode, Raw = raw ?? string.Empty });
                // #endregion
                return new PaymentGatewayResult
                {
                    Aprobado = false,
                    Estado = "REC",
                    Mensaje = $"Pago rechazado por la pasarela ({(int)response.StatusCode}).",
                    RespuestaRaw = raw ?? string.Empty
                };
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            }
            catch (Exception ex)
            {
                // #region agent log
                WriteDebugLog(
                    "H27",
                    "Facturacion.API/Services/HttpPaymentGateway.cs:ProcesarPagoAsync:parseError",
                    "Gateway response parse failed",
                    new { ex.Message, Raw = raw ?? string.Empty });
                // #endregion
                return new PaymentGatewayResult
                {
                    Aprobado = false,
                    Estado = "REC",
                    Mensaje = "La pasarela devolvio una respuesta invalida. Intenta nuevamente.",
                    RespuestaRaw = raw ?? string.Empty
                };
            }
            using (document)
            {
                var root = document.RootElement;
                var status = ReadString(root, "status", "estado", "state").ToUpperInvariant();
                var approved = status is "APPROVED" or "APROBADO" or "APR" or "PAID" or "PAGADO" or "OK" or "SUCCESS";

                return new PaymentGatewayResult
                {
                    Aprobado = approved,
                    Estado = approved ? "APR" : "REC",
                    TransaccionExterna = ReadString(root, "transactionId", "transaccionExterna", "id"),
                    CodigoAutorizacion = ReadString(root, "authorizationCode", "codigoAutorizacion", "authCode"),
                    Mensaje = ReadString(root, "message", "mensaje") is { Length: > 0 } msg
                        ? msg
                        : approved ? "Pago aprobado por la pasarela." : "Pago rechazado por la pasarela.",
                    RespuestaRaw = raw
                };
            }
        }

        private static string ReadString(JsonElement root, params string[] names)
        {
            foreach (var name in names)
            {
                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty(name, out var value) &&
                    value.ValueKind != JsonValueKind.Null &&
                    value.ValueKind != JsonValueKind.Undefined)
                {
                    return value.ToString() ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private static PaymentGatewayResult CrearPagoSimulado(PaymentGatewayRequest request)
        {
            var transactionId = $"SIM-{Guid.NewGuid():N}";
            var authorizationCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

            return new PaymentGatewayResult
            {
                Aprobado = true,
                Estado = "APR",
                TransaccionExterna = transactionId,
                CodigoAutorizacion = authorizationCode,
                Mensaje = "Pago aprobado en modo simulacion.",
                RespuestaRaw = JsonSerializer.Serialize(new
                {
                    status = "APPROVED",
                    mode = "SIMULATED",
                    transactionId,
                    authorizationCode,
                    amount = request.Monto,
                    currency = request.Moneda,
                    reference = request.Referencia,
                    reservationId = request.IdReserva,
                    invoiceId = request.IdFactura
                })
            };
        }
    }
}
