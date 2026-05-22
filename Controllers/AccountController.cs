using GMT.Data;
using GMT.Models;
using GMT.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GMT.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext context,
            EmailService emailService,
            ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index() => View();

        // ══════════════════════════════════════════════════════════════════════
        //  POST /Account/Login
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password, string TipoRegistro)
        {
            try
            {
                var loginUsuario = await _context.Logins
                    .FirstOrDefaultAsync(l => l.CorreoInstitucional == Email);

                if (loginUsuario == null)
                {
                    _logger.LogWarning("Acceso fallido: correo no encontrado {Email}", Email);
                    ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                    return View("Index");
                }

                bool passwordValido = BCrypt.Net.BCrypt.Verify(Password, loginUsuario.PasswordHash);
                if (!passwordValido)
                {
                    _logger.LogWarning("Acceso fallido: contraseña incorrecta para {Email}", Email);
                    ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                    return View("Index");
                }

                var rolReal = loginUsuario.Rol; // 'alumno' | 'empresa' | 'admin'

                // ── Verificar lista negra (solo empresas) ─────────────────────
                if (rolReal == "empresa")
                {
                    var empresa = await _context.Empresas
                        .FirstOrDefaultAsync(e => e.LoginId == loginUsuario.Id);

                    if (empresa != null)
                    {
                        var enListaNegra = await _context.ListaNegraEmpresas
                            .AnyAsync(l => l.EmpresaId == empresa.Id && l.Activo);

                        if (enListaNegra)
                        {
                            _logger.LogWarning("Intento de acceso bloqueado — empresa en lista negra. LoginId={Id}", loginUsuario.Id);
                            ModelState.AddModelError("", "Tu cuenta ha sido suspendida. Contacta a Vinculación en vinculacion@gmtek.lol para más información.");
                            return View("Index");
                        }
                    }
                }

                // ── Crear sesión ──────────────────────────────────────────────
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Email),
                    new Claim(ClaimTypes.Role, rolReal),
                    new Claim("UserType", TipoRegistro)
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                // Actualizar último acceso
                loginUsuario.UltimoAcceso = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario {Email} inició sesión ({Rol})", Email, rolReal);

                // ── Redirección según rol ─────────────────────────────────────
                return rolReal switch
                {
                    "admin" => RedirectToAction("Index", "Admin"),
                    "empresa" => RedirectToAction("Empresa", "Dashboard"),
                    _ => RedirectToAction("Alumno", "Dashboard")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante Login");
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

        [HttpGet]
        public IActionResult PendingVerification() => View();

        [HttpGet]
        public IActionResult RegistrationSuccess() => View();

        // ══════════════════════════════════════════════════════════════════════
        //  POST /Account/Register
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("Account/Register")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Register(
            string TipoRegistro, string Email, string Password,
            // Alumno
            string? NombreCompleto, string? Matricula, string? Carrera,
            int? Semestre, string? Telefono,
            // Empresa
            string? NombreEmpresa, string? RazonSocial, string? RFC,
            string? Sector, string? Giro, string? Ciudad,
            string? NombreContacto, string? PuestoContacto, string? TelefonoContacto)
        {
            try
            {
                // Verificar que el correo no exista ya
                var yaRegistrado = await _context.Logins
                    .AnyAsync(l => l.CorreoInstitucional == Email);

                if (yaRegistrado)
                {
                    ModelState.AddModelError("", "Este correo ya está registrado. Intenta iniciar sesión.");
                    return View("Index");
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

                var datosRegistro = new
                {
                    CorreoElectronico = Email,
                    PasswordHash = passwordHash,
                    TipoRegistro,
                    NombreCompleto,
                    Matricula,
                    Carrera,
                    Semestre,
                    Telefono,
                    NombreEmpresa,
                    RazonSocial,
                    RFC,
                    Sector,
                    Giro,
                    Ciudad,
                    NombreContacto,
                    PuestoContacto,
                    TelefonoContacto
                };

                var registroPendiente = new RegistroPendiente
                {
                    Id = Guid.NewGuid(),
                    Email = Email,
                    TipoRegistro = TipoRegistro,
                    DatosJson = JsonSerializer.Serialize(datosRegistro),
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
                };

                _context.RegistrosPendientes.Add(registroPendiente);
                await _context.SaveChangesAsync();

                string verificationLink = Url.Action(
                    "Verify", "Verification",
                    new { token = registroPendiente.Id.ToString() },
                    Request.Scheme) ?? "";

                await _emailService.SendVerificationEmailAsync(Email, verificationLink);

                return RedirectToAction("PendingVerification");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en registro de usuario");
                return View("Index");
            }
        }
    }
}
