using GMT.Models;
using GMT.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;
using BCrypt.Net;

namespace GMT.Controllers
{
    public class EmpresaAuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRfcValidationService _rfcValidationService;
        private readonly EmailService _emailService;

        public EmpresaAuthController(
            ApplicationDbContext context,
            IRfcValidationService rfcValidationService,
            EmailService emailService)
        {
            _context = context;
            _rfcValidationService = rfcValidationService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string nombreEmpresa, string rfc, string correoElectronico, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(nombreEmpresa) || string.IsNullOrWhiteSpace(rfc) ||
                string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Todos los campos son obligatorios.");
                return View();
            }

            if (rfc.Length != 12)
            {
                ModelState.AddModelError(nameof(rfc), "El RFC debe tener exactamente 12 caracteres (solo Personas Morales).");
            }

            if (!password.Equals(confirmPassword))
            {
                ModelState.AddModelError(nameof(confirmPassword), "Las contraseñas no coinciden.");
            }

            var existingEmpresa = await _context.Empresas.FirstOrDefaultAsync(e => e.CorreoElectronico == correoElectronico);
            var existingLogin = await _context.Logins.FirstOrDefaultAsync(l => l.CorreoInstitucional == correoElectronico);
            if (existingEmpresa != null || existingLogin != null)
            {
                ModelState.AddModelError(nameof(correoElectronico), "El correo electrónico ya está registrado.");
            }

            if (!ModelState.IsValid) return View();

            if (rfc.Length == 12)
            {
                bool isRfcValid = await _rfcValidationService.ValidarRfcSatAsync(rfc);
                if (!isRfcValid)
                {
                    ModelState.AddModelError(nameof(rfc), "El RFC proporcionado no se encuentra activo ante el SAT.");
                    return View();
                }
            }

            // Hashear la contraseña (mejorar a PBKDF2/BCrypt después)
            string hashedPassword = HashPassword(password);

            // Construir JSON de preregistro
            var payload = new
            {
                TipoRegistro = "Empresa",
                NombreEmpresa = nombreEmpresa,
                RFC = rfc,
                CorreoElectronico = correoElectronico,
                PasswordHash = hashedPassword,
                S3Key = (string?)null
            };
            var datosJson = JsonSerializer.Serialize(payload);

            var token = Guid.NewGuid();
            var registroPendiente = new RegistroPendiente
            {
                Id = token,
                Email = correoElectronico,
                DatosJson = datosJson,
                TipoRegistro = "Empresa",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.RegistrosPendientes.Add(registroPendiente);
                    await _context.SaveChangesAsync();

                    string verificationLink = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/verify?token={token}";

                    try
                    {
                        await _emailService.SendVerificationEmailAsync(registroPendiente.Email, verificationLink);
                    }
                    catch (Exception mailEx)
                    {
                        await transaction.RollbackAsync();
                        System.Diagnostics.Debug.WriteLine($"Error de AWS SES: {mailEx.Message}");
                        return StatusCode(500, "No se pudo enviar el correo de verificación. El registro no se completó.");
                    }

                    await transaction.CommitAsync();
                    return RedirectToAction("RegistrationSuccess");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty, $"Error al procesar el registro: {ex.Message}");
                    return View();
                }
            }
        }

        [HttpGet]
        public IActionResult RegistrationSuccess()
        {
            return View();
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
