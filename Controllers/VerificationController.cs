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
        /// GET: /Verification/Verify?token=xxx
        /// Valida el token y promociona el registro pendiente a login+alumno/empresa.
        /// </summary>
        [HttpGet("verify")]
        public async Task<IActionResult> Verify(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || !Guid.TryParse(token, out var tokenGuid))
            {
                ViewBag.Message = "El enlace de verificación no es válido.";
                // Ruta absoluta — la vista vive en Views/Account/, no en Views/Verification/
                return View("~/Views/Account/RegistrationFailed.cshtml");
            }

            var registro = await _verificationService.ObtenerRegistroPendienteActivoAsync(tokenGuid);
            if (registro == null)
            {
                ViewBag.Message = "El enlace ha expirado o ya fue utilizado. Por favor regístrate nuevamente.";
                return View("~/Views/Account/RegistrationFailed.cshtml");
            }

            var success = await _verificationService.ConfirmarRegistroAsync(tokenGuid);
            if (!success)
            {
                ViewBag.Message = "Ocurrió un error al procesar tu verificación. Inténtalo de nuevo.";
                return View("~/Views/Account/RegistrationFailed.cshtml");
            }

            return RedirectToAction("RegistrationSuccess", "Account");
        }

        /// <summary>
        /// GET: /Verification/RegistrationFailed
        /// Pantalla de error accesible directamente si se necesita.
        /// </summary>
        [HttpGet("verify/failed")]
        public IActionResult RegistrationFailed()
        {
            return View("~/Views/Account/RegistrationFailed.cshtml");
        }
    }
}
