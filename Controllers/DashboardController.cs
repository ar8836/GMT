using GMT.Data;
using GMT.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace GMT.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Obtiene el Login del usuario autenticado. Redirige a login si no existe.</summary>
        private async Task<Login?> GetLoginAsync()
        {
            var correo = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(correo)) return null;
            return await _context.Logins.FirstOrDefaultAsync(l => l.CorreoInstitucional == correo);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PORTAL ALUMNO
        // ══════════════════════════════════════════════════════════════════════

        [Authorize(Roles = "alumno")]
        public async Task<IActionResult> Alumno()
        {
            var login = await GetLoginAsync();
            if (login == null) return RedirectToAction("Index", "Account");

            var alumno = await _context.Alumnos
                .FirstOrDefaultAsync(a => a.LoginId == login.Id);

            ViewData["UserName"] = alumno?.NombreCompleto ?? login.CorreoInstitucional;
            ViewData["UserRole"] = alumno != null ? $"Alumno · {alumno.Carrera}" : "Alumno";
            ViewData["ActiveModule"] = "dashboard";

            // Estadísticas básicas
            if (alumno != null)
            {
                var solicitudes = await _context.SolicitudesPracticas
                    .Where(s => s.AlumnoId == alumno.Id)
                    .ToListAsync();

                ViewBag.TotalSolicitudes = solicitudes.Count;
                ViewBag.SolicitudActiva = solicitudes.FirstOrDefault(s => s.Estado == "en_curso" || s.Estado == "aceptado");
                ViewBag.TotalDocumentos = await _context.DocumentosAlumno.CountAsync(d => d.AlumnoId == alumno.Id);
                ViewBag.Alumno = alumno;
            }

            return View("Alumno/Index");
        }

        [Authorize(Roles = "alumno")]
        public async Task<IActionResult> PerfilAcademico()
        {
            var login = await GetLoginAsync();
            if (login == null) return RedirectToAction("Index", "Account");

            var alumno = await _context.Alumnos
                .Include(a => a.Login)
                .FirstOrDefaultAsync(a => a.LoginId == login.Id);

            ViewData["UserName"] = alumno?.NombreCompleto ?? login.CorreoInstitucional;
            ViewData["UserRole"] = alumno != null ? $"Alumno · {alumno.Carrera}" : "Alumno";
            ViewData["ActiveModule"] = "perfil";
            ViewBag.Alumno = alumno;

            return View("Alumno/PerfilAcademico");
        }

        [Authorize(Roles = "alumno")]
        public async Task<IActionResult> Peticiones()
        {
            var login = await GetLoginAsync();
            if (login == null) return RedirectToAction("Index", "Account");

            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.LoginId == login.Id);

            ViewData["UserName"] = alumno?.NombreCompleto ?? login.CorreoInstitucional;
            ViewData["UserRole"] = alumno != null ? $"Alumno · {alumno.Carrera}" : "Alumno";
            ViewData["ActiveModule"] = "peticiones";

            if (alumno != null)
            {
                var solicitudes = await _context.SolicitudesPracticas
                    .Include(s => s.Empresa)
                    .Include(s => s.Plaza)
                    .Where(s => s.AlumnoId == alumno.Id)
                    .OrderByDescending(s => s.FechaSolicitud)
                    .ToListAsync();

                ViewBag.Solicitudes = solicitudes;

                // Plazas activas disponibles para el alumno (para el modal "Nueva Solicitud")
                var plazasDisponibles = await _context.PlazasPracticas
                    .Include(p => p.Empresa)
                    .Where(p => p.Estado == "activa" && p.CuposOcupados < p.CuposDisponibles)
                    .OrderByDescending(p => p.FechaPublicacion)
                    .Take(20)
                    .ToListAsync();

                ViewBag.PlazasDisponibles = plazasDisponibles;
            }

            return View("Alumno/Peticiones");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PORTAL EMPRESA
        // ══════════════════════════════════════════════════════════════════════

        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> Empresa()
        {
            var login = await GetLoginAsync();
            if (login == null) return RedirectToAction("Index", "Account");

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.LoginId == login.Id);

            if (empresa == null) return RedirectToAction("Index", "Account");

            SetEmpresaViewData(empresa, "dashboard");

            // KPIs del dashboard
            var plazas = await _context.PlazasPracticas
                .Where(p => p.EmpresaId == empresa.Id)
                .ToListAsync();

            var solicitudes = await _context.SolicitudesPracticas
                .Include(s => s.Alumno)
                .Include(s => s.Plaza)
                .Where(s => s.EmpresaId == empresa.Id)
                .OrderByDescending(s => s.FechaSolicitud)
                .Take(5)
                .ToListAsync();

            ViewBag.Empresa = empresa;
            ViewBag.PlazasActivas = plazas.Count(p => p.Estado == "activa");
            ViewBag.PlazasBorrador = plazas.Count(p => p.Estado == "borrador");
            ViewBag.TotalSolicitudes = await _context.SolicitudesPracticas.CountAsync(s => s.EmpresaId == empresa.Id);
            ViewBag.SolicitudesPendientes = await _context.SolicitudesPracticas.CountAsync(s => s.EmpresaId == empresa.Id && s.Estado == "solicitado");
            ViewBag.UltimasSolicitudes = solicitudes;
            ViewBag.TieneDocumentos = await _context.DocumentosEmpresa.AnyAsync(d => d.EmpresaId == empresa.Id);

            return View("Empresa/Index");
        }

        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> EmpresaPlazas()
        {
            var login = await GetLoginAsync();
            if (login == null) return RedirectToAction("Index", "Account");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login.Id);
            if (empresa == null) return RedirectToAction("Index", "Account");

            SetEmpresaViewData(empresa, "plazas");

            var plazas = await _context.PlazasPracticas
                .Where(p => p.EmpresaId == empresa.Id)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            ViewBag.Empresa = empresa;
            ViewBag.Plazas = plazas;

            return View("Empresa/Plazas");
        }

        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> EmpresaSolicitudes()
        {
            var login = await GetLoginAsync();
            if (login == null) return RedirectToAction("Index", "Account");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login.Id);
            if (empresa == null) return RedirectToAction("Index", "Account");

            SetEmpresaViewData(empresa, "solicitudes");

            var solicitudes = await _context.SolicitudesPracticas
                .Include(s => s.Alumno)
                .Include(s => s.Plaza)
                .Where(s => s.EmpresaId == empresa.Id)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            ViewBag.Empresa = empresa;
            ViewBag.Solicitudes = solicitudes;

            return View("Empresa/Solicitudes");
        }

        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> EmpresaDocumentos()
        {
            var login = await GetLoginAsync();
            if (login == null) return RedirectToAction("Index", "Account");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login.Id);
            if (empresa == null) return RedirectToAction("Index", "Account");

            SetEmpresaViewData(empresa, "documentos");

            var documentos = await _context.DocumentosEmpresa
                .Where(d => d.EmpresaId == empresa.Id)
                .OrderByDescending(d => d.FechaSubida)
                .ToListAsync();

            ViewBag.Empresa = empresa;
            ViewBag.Documentos = documentos;

            return View("Empresa/Documentos");
        }

        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> EmpresaPerfil()
        {
            var login = await GetLoginAsync();
            if (login == null) return RedirectToAction("Index", "Account");

            var empresa = await _context.Empresas
                .Include(e => e.Login)
                .FirstOrDefaultAsync(e => e.LoginId == login.Id);

            if (empresa == null) return RedirectToAction("Index", "Account");

            SetEmpresaViewData(empresa, "perfil");
            ViewBag.Empresa = empresa;

            return View("Empresa/Perfil");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ACCIONES API (AJAX) — PLAZAS
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost]
        [Authorize(Roles = "empresa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPlaza(
            string Titulo, string Descripcion, string? Area,
            string Modalidad, int? SemestreMinimo, string? CarrerasRequeridas,
            int CuposDisponibles, string? FechaInicio, string? FechaFin,
            bool PublicarAhora)
        {
            var login = await GetLoginAsync();
            if (login == null) return Unauthorized();

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login.Id);
            if (empresa == null) return Unauthorized();

            var plaza = new PlazaPractica
            {
                EmpresaId = empresa.Id,
                Titulo = Titulo,
                Descripcion = Descripcion,
                Area = Area,
                Modalidad = Modalidad,
                SemestreMinimo = SemestreMinimo,
                CarrerasRequeridas = CarrerasRequeridas,
                CuposDisponibles = Math.Max(1, CuposDisponibles),
                FechaInicio = string.IsNullOrEmpty(FechaInicio) ? null : DateOnly.Parse(FechaInicio),
                FechaFin = string.IsNullOrEmpty(FechaFin) ? null : DateOnly.Parse(FechaFin),
                Estado = PublicarAhora ? "activa" : "borrador",
                FechaPublicacion = PublicarAhora ? DateTimeOffset.UtcNow : null
            };

            _context.PlazasPracticas.Add(plaza);
            await _context.SaveChangesAsync();

            return RedirectToAction("EmpresaPlazas");
        }

        [HttpPost]
        [Authorize(Roles = "empresa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoPlaza(int plazaId, string nuevoEstado)
        {
            var login = await GetLoginAsync();
            if (login == null) return Unauthorized();

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login.Id);
            var plaza = await _context.PlazasPracticas.FindAsync(plazaId);

            if (plaza == null || plaza.EmpresaId != empresa?.Id)
                return NotFound();

            var estadosValidos = new[] { "activa", "pausada", "cerrada", "borrador" };
            if (!estadosValidos.Contains(nuevoEstado)) return BadRequest();

            plaza.Estado = nuevoEstado;
            plaza.FechaActualizacion = DateTimeOffset.UtcNow;

            if (nuevoEstado == "activa" && plaza.FechaPublicacion == null)
                plaza.FechaPublicacion = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("EmpresaPlazas");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ACCIONES API (AJAX) — SOLICITUDES (empresa responde)
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost]
        [Authorize(Roles = "empresa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResponderSolicitud(int solicitudId, string decision)
        {
            var login = await GetLoginAsync();
            if (login == null) return Unauthorized();

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login.Id);
            var sol = await _context.SolicitudesPracticas
                .Include(s => s.Plaza)
                .FirstOrDefaultAsync(s => s.Id == solicitudId);

            if (sol == null || sol.EmpresaId != empresa?.Id) return NotFound();

            if (decision == "aceptado")
            {
                sol.Estado = "aceptado";
                // Incrementar cupos ocupados en la plaza
                if (sol.PlazaId.HasValue && sol.Plaza != null)
                {
                    sol.Plaza.CuposOcupados++;
                    if (sol.Plaza.CuposOcupados >= sol.Plaza.CuposDisponibles)
                        sol.Plaza.Estado = "cerrada";
                }
            }
            else if (decision == "rechazado")
            {
                sol.Estado = "rechazado";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("EmpresaSolicitudes");
        }

        [HttpPost]
        [Authorize(Roles = "empresa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPerfilEmpresa(
            string NombreEmpresa, string? RazonSocial, string? RFC,
            string? Ciudad, string? Sector, string? Giro,
            string? Direccion, string? NombreContacto,
            string? PuestoContacto, string? TelefonoContacto)
        {
            var login = await GetLoginAsync();
            if (login == null) return RedirectToAction("Index", "Account");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login.Id);
            if (empresa == null) return RedirectToAction("Index", "Account");

            empresa.NombreEmpresa = NombreEmpresa;
            empresa.RazonSocial = RazonSocial;
            if (!string.IsNullOrWhiteSpace(RFC)) empresa.RFC = RFC.Trim().ToUpper();
            empresa.Ciudad = Ciudad;
            empresa.Sector = Sector;
            empresa.Giro = Giro;
            empresa.Direccion = Direccion;
            empresa.NombreContacto = NombreContacto;
            empresa.PuestoContacto = PuestoContacto;
            empresa.TelefonoContacto = TelefonoContacto;

            // Marcar perfil completo si tiene los campos mínimos
            empresa.DatosCompletos =
                !string.IsNullOrEmpty(empresa.NombreEmpresa) &&
                !string.IsNullOrEmpty(empresa.RFC) &&
                !string.IsNullOrEmpty(empresa.NombreContacto);

            await _context.SaveChangesAsync();
            return RedirectToAction("EmpresaPerfil");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPER PRIVADO
        // ══════════════════════════════════════════════════════════════════════

        private void SetEmpresaViewData(Empresa empresa, string activeModule)
        {
            ViewData["UserName"] = empresa.NombreEmpresa;
            ViewData["UserRole"] = $"Empresa · {empresa.Sector ?? empresa.Giro ?? "Empresa"}";
            ViewData["ActiveModule"] = activeModule;
            ViewData["PortalTipo"] = "empresa";
        }
    }
}
