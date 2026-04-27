using GMT.Models;
using GMT.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GMT.Controllers
{
    public class EmpresaAuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRfcValidationService _rfcValidationService;
        private readonly EmailService _emailService;
        private readonly S3Service _s3Service;
        private readonly IConfiguration _configuration;

        public EmpresaAuthController(
            ApplicationDbContext context,
            IRfcValidationService rfcValidationService,
            EmailService emailService,
            S3Service s3Service,
            IConfiguration configuration)
        {
            _context = context;
            _rfcValidationService = rfcValidationService;
            _emailService = emailService;
            _s3Service = s3Service;
            _configuration = configuration;
        }

        /// <summary>
        /// GET: /EmpresaAuth/Register - Muestra el formulario de registro
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// POST: /EmpresaAuth/Register - Procesa el registro de empresa con validación de doble capa
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string nombreEmpresa, string rfc, string correoElectronico, string password, string confirmPassword)
        {
            // 1. Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(nombreEmpresa) || string.IsNullOrWhiteSpace(rfc) ||
                string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Todos los campos son obligatorios.");
                return View();
            }

            // 2. Capa 1: Validar longitud del RFC
            if (rfc.Length == 13)
            {
                ModelState.AddModelError(nameof(rfc), "Solo se permiten registros de Personas Morales (12 caracteres).");
            }
            else if (rfc.Length != 12)
            {
                ModelState.AddModelError(nameof(rfc), "El RFC debe tener exactamente 12 caracteres.");
            }

            // 3. Validar coincidencia de contraseñas
            if (!password.Equals(confirmPassword))
            {
                ModelState.AddModelError(nameof(confirmPassword), "Las contraseñas no coinciden.");
            }

            // 4. Verificar que el correo no esté ya registrado
            var existingEmpresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.CorreoElectronico == correoElectronico);
            if (existingEmpresa != null)
            {
                ModelState.AddModelError(nameof(correoElectronico), "El correo electrónico ya está registrado.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            // 5. Capa 2: Validar RFC ante el SAT (vía API sandbox de Facturama)
            if (rfc.Length == 12)
            {
                bool isRfcValid = await _rfcValidationService.ValidarRfcSatAsync(rfc);
                if (!isRfcValid)
                {
                    ModelState.AddModelError(nameof(rfc), "El RFC proporcionado no se encuentra activo ante el SAT.");
                    return View();
                }
            }

            // 6. Hash de la contraseña con SHA256
            string hashedPassword = HashPassword(password);

            // 7. Crear entidad de empresa con estado no verificado
            var empresa = new Empresa
            {
                NombreEmpresa = nombreEmpresa,
                RFC = rfc,
                CorreoElectronico = correoElectronico,
                PasswordHash = hashedPassword,
                EstaVerificado = false,
                FechaRegistro = DateTime.UtcNow
            };

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 8. Guardar en la base de datos dentro de la transacción
                    _context.Empresas.Add(empresa);
                    await _context.SaveChangesAsync();

                    // 9. Generar enlace de verificación único
                    string verificationLink = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/EmpresaAuth/Verify?token={Guid.NewGuid()}";

                    // 10. Enviar correo de verificación (obligatorio)
                    try
                    {
                        await _emailService.SendVerificationEmailAsync(empresa.CorreoElectronico, verificationLink);
                    }
                    catch (Exception mailEx)
                    {
                        // Si el correo falla, revertir la transacción
                        await transaction.RollbackAsync();
                        System.Diagnostics.Debug.WriteLine($"Error de AWS SES: {mailEx.Message}");
                        return StatusCode(500, "No se pudo enviar el correo de verificación. El registro no se completó.");
                    }

                    // 11. Confirmar la transacción si todo salió bien
                    await transaction.CommitAsync();
                    return RedirectToAction("RegistrationSuccess");
                }
                catch (Exception ex)
                {
                    // Si cualquier paso falla, revertir la transacción
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty, $"Error al procesar el registro: {ex.Message}");
                    return View();
                }
            }
        }

        /// <summary>
        /// GET: /EmpresaAuth/Verify?token=xxx - Valida el token de verificación de cuenta
        /// </summary>
        [HttpGet]
        public IActionResult Verify(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Message = "Token de verificación inválido.";
                return View();
            }

            // TODO: Buscar en base de datos el token y activar la cuenta (empresa.EstaVerificado = true)
            ViewBag.Message = "Cuenta verificada exitosamente. Ya puedes iniciar sesión.";
            return View();
        }

        /// <summary>
        /// GET: /EmpresaAuth/RegistrationSuccess - Página de confirmación tras registro exitoso
        /// </summary>
        [HttpGet]
        public IActionResult RegistrationSuccess()
        {
            return View();
        }

        /// <summary>
        /// Método privado para hashear contraseñas usando SHA256
        /// </summary>
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
