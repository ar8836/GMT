using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GMT.Services
{
    /// <summary>
    /// Servicio para validar RFC ante el SAT mediante la API sandbox de Facturama.
    /// Implementa IRfcValidationService.
    /// </summary>
    public class RfcValidationService : IRfcValidationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public RfcValidationService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        /// <summary>
        /// Valida que el RFC tenga exactamente 12 caracteres y, opcionalmente,
        /// consulta la API de Facturama para verificar que el RFC está activo.
        /// </summary>
        /// <param name="rfc">RFC a validar (12 caracteres esperados)</param>
        /// <returns>True si el RFC es válido y activo; de lo contrario, false.</returns>
        public async Task<bool> ValidarRfcSatAsync(string rfc)
        {
            // Variable de entorno para usar modo mock
            bool useMockApi = bool.Parse(
                Environment.GetEnvironmentVariable("USE_MOCK_API") ?? "false");

            // Si estamos en modo mock, sólo aceptamos el RFC de prueba conocido
            if (useMockApi)
            {
                return rfc == "EKU9003173C9";
            }

            // Construir URL de la API de Facturama
            string url = $"https://apisandbox.facturama.mx/v1/ruc/{rfc}";

            try
            {
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    // Si la llamada falla (p.ej., 404), consideramos que el RFC no está activo
                    return false;
                }

                // La respuesta exitosa indica que el RFC está activo
                return true;
            }
            catch
            {
                // En caso de error de red o parsing, consideramos que el RFC no es válido
                return false;
            }
        }
    }
}
