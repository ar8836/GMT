using GMT.Models;
using GMT.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BCrypt.Net;

namespace GMT.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, EmailService emailService, ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index() => View();

        // --- LÓGICA DE LOGIN (POST) ---[cite: 13, 14]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password, string TipoRegistro)
        {
            try
            {
                // 1. Buscar al usuario en la tabla de Logins[cite: 13, 16]
                var loginUsuario = await _context.Logins
                    .FirstOrDefaultAsync(l => l.CorreoInstitucional == Email);

                if (loginUsuario == null)
                {
                    _logger.LogWarning("Intento de acceso fallido: Correo no encontrado {Email}", Email);
                    ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                    return View("Index");
                }

                // 2. Verificar contraseña con BCrypt[cite: 13, 16]
                bool passwordValido = BCrypt.Net.BCrypt.Verify(Password, loginUsuario.PasswordHash);

                if (!passwordValido)
                {
                    _logger.LogWarning("Intento de acceso fallido: Contraseña incorrecta para {Email}", Email);
                    ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                    return View("Index");
                }

                // 3. (Opcional) Verificar si el usuario ya confirmó su correo[cite: 12]
                // if (!loginUsuario.EstaVerificado) return RedirectToAction("PendingVerification");

                // 4. Crear la identidad del usuario (Claims) para la sesión
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Email),
                    new Claim("UserType", TipoRegistro) // Guardamos si es Alumno o Empresa
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                _logger.LogInformation("Usuario {Email} inició sesión como {Tipo}", Email, TipoRegistro);

                // 5. REDIRECCIONAMIENTO CORRECTO[cite: 13, 15]
                // Si es Alumno, va a Dashboard/Alumno. Si es Empresa, a Dashboard/Empresa.
                if (TipoRegistro == "Alumno")
                {
                    return RedirectToAction("Alumno", "Dashboard");
                }
                else
                {
                    return RedirectToAction("Empresa", "Dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el proceso de Login");
                ModelState.AddModelError("", "Ocurrió un error inesperado. Inténtalo de nuevo.");
                return View("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Account");
        }

        // --- LÓGICA DE REGISTRO ---[cite: 16]
        [HttpGet]
        public IActionResult PendingVerification() => View();

        [HttpGet]
        public IActionResult RegistrationSuccess() => View();

        [HttpPost("Account/Register")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Register(
            string TipoRegistro, string Email, string Password,
            string? NombreCompleto, string? Institucion, string? Matricula, string? Carrera,
            int? Semestre, string? NombreEmpresa, string? RFC, string? Sector)
        {
            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

                var datosRegistro = new
                {
                    CorreoElectronico = Email,
                    PasswordHash = passwordHash,
                    TipoRegistro = TipoRegistro,
                    NombreCompleto,
                    Institucion,
                    Matricula,
                    Carrera,
                    Semestre = Semestre ?? 1
                };

                var registroPendiente = new RegistroPendiente
                {
                    Id = Guid.NewGuid(),
                    Email = Email,
                    TipoRegistro = TipoRegistro,
                    DatosJson = JsonSerializer.Serialize(datosRegistro),
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                _context.RegistrosPendientes.Add(registroPendiente);
                await _context.SaveChangesAsync();

                string verificationLink = Url.Action("Verify", "Verification",
                    new { token = registroPendiente.Id.ToString() }, Request.Scheme) ?? "";

                await _emailService.SendVerificationEmailAsync(Email, verificationLink);

                return RedirectToAction("PendingVerification");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el registro de usuario");
                return View("Index");
            }
        }
    }
}