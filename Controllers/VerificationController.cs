using GMT.Models;
using GMT.Services;
using Microsoft.AspNetCore.Mvc;

namespace GMT.Controllers
{
    public class VerificationController : Controller
    {
        private readonly IVerificationService _verificationService;
        private readonly EmailService _emailService;

        public VerificationController(
            IVerificationService verificationService,
            EmailService emailService)
        {
            _verificationService = verificationService;
            _emailService = emailService;
        }

        /// <summary>
        /// GET: /Verification/Verify?token=xxx - Valida el token y promociona el registro
        /// </summary>
        [HttpGet("verify")]
        public async Task<IActionResult> Verify(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || !Guid.TryParse(token, out var tokenGuid))
            {
                ViewBag.Message = "Token de verificación inválido.";
                return View("RegistrationFailed");
            }

            // Obtener registro pendiente activo
            var registro = await _verificationService.ObtenerRegistroPendienteActivoAsync(tokenGuid);
            if (registro == null)
            {
                ViewBag.Message = "El token ha expirado o no existe. Regístrese nuevamente.";
                return View("RegistrationFailed");
            }

            // Confirmar y promocionar el registro
            var success = await _verificationService.ConfirmarRegistroAsync(tokenGuid);
            if (!success)
            {
                ViewBag.Message = "Error al procesar la verificación. Intente nuevamente.";
                return View("RegistrationFailed");
            }

            return RedirectToAction("RegistrationSuccess", "EmpresaAuth");
        }

        /// <summary>
        /// GET: /Verification/RegistrationFailed - Página de error de verificación
        /// </summary>
        [HttpGet("verify/failed")]
        public IActionResult RegistrationFailed()
        {
            return View();
        }
    }
}
