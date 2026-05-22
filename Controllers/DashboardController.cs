using GMT.Data;
using GMT.Models;
using GMT.Services;
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
        private readonly EmailService _emailService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context,
            EmailService emailService,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

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

            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.LoginId == login.Id);

            ViewData["UserName"] = alumno?.NombreCompleto ?? login.CorreoInstitucional;
            ViewData["UserRole"] = alumno != null ? $"Alumno · {alumno.Carrera}" : "Alumno";
            ViewData["ActiveModule"] = "dashboard";

            if (alumno != null)
            {
                var solicitudes = await _context.SolicitudesPracticas
                    .Where(s => s.AlumnoId == alumno.Id).ToListAsync();

                ViewBag.TotalSolicitudes = solicitudes.Count;
                ViewBag.SolicitudActiva = solicitudes.FirstOrDefault(s => s.Estado == "en_curso" || s.Estado == "aceptado");
                ViewBag.TotalDocumentos = await _context.DocumentosAlumno.CountAsync(d => d.AlumnoId == alumno.Id);
                ViewBag.Alumno = alumno;

                // Próximas entrevistas del alumno
                ViewBag.Entrevistas = await _context.Entrevistas
                    .Include(e => e.Empresa)
                    .Include(e => e.Solicitud).ThenInclude(s => s!.Plaza)
                    .Where(e => e.AlumnoId == alumno.Id && e.Estado != "cancelada" && e.FechaHora >= DateTimeOffset.UtcNow)
                    .OrderBy(e => e.FechaHora)
                    .Take(3)
                    .ToListAsync();
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
                .Include(a => a.Documentos)
                .FirstOrDefaultAsync(a => a.LoginId == login.Id);

            ViewData["UserName"] = alumno?.NombreCompleto ?? login.CorreoInstitucional;
            ViewData["UserRole"] = alumno != null ? $"Alumno · {alumno.Carrera}" : "Alumno";
            ViewData["ActiveModule"] = "perfil";
            ViewBag.Alumno = alumno;

            // Solo CV y constancia de estudios
            ViewBag.Documentos = alumno?.Documentos
                .Where(d => d.TipoDocumento == "cv" || d.TipoDocumento == "constancia_estudios")
                .OrderByDescending(d => d.FechaSubida)
                .ToList() ?? new List<DocumentoAlumno>();

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
                ViewBag.Solicitudes = await _context.SolicitudesPracticas
                    .Include(s => s.Empresa)
                    .Include(s => s.Plaza)
                    .Where(s => s.AlumnoId == alumno.Id)
                    .OrderByDescending(s => s.FechaSolicitud)
                    .ToListAsync();

                // Plazas activas disponibles
                ViewBag.PlazasDisponibles = await _context.PlazasPracticas
                    .Include(p => p.Empresa)
                    .Where(p => p.Estado == "activa" && p.CuposOcupados < p.CuposDisponibles)
                    .OrderByDescending(p => p.FechaPublicacion)
                    .Take(50)
                    .ToListAsync();

                ViewBag.AlumnoId = alumno.Id;

                // Documentos del alumno (para mostrar en modal de aplicar)
                ViewBag.DocsCv = await _context.DocumentosAlumno
                    .Where(d => d.AlumnoId == alumno.Id && d.TipoDocumento == "cv")
                    .OrderByDescending(d => d.FechaSubida)
                    .FirstOrDefaultAsync();

                ViewBag.DocsConstancia = await _context.DocumentosAlumno
                    .Where(d => d.AlumnoId == alumno.Id && d.TipoDocumento == "constancia_estudios")
                    .OrderByDescending(d => d.FechaSubida)
                    .FirstOrDefaultAsync();
            }

            return View("Alumno/Peticiones");
        }

        // ── Aplicar a una plaza ───────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "alumno")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AplicarSolicitud(int plazaId)
        {
            var login = await GetLoginAsync();
            if (login == null) return Unauthorized();

            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.LoginId == login.Id);
            if (alumno == null) return Unauthorized();

            var plaza = await _context.PlazasPracticas
                .Include(p => p.Empresa)
                .FirstOrDefaultAsync(p => p.Id == plazaId);

            if (plaza == null || plaza.Estado != "activa")
                return BadRequest(new { error = "La plaza no está disponible" });

            // Verificar que no haya aplicado ya
            var yaAplicó = await _context.SolicitudesPracticas
                .AnyAsync(s => s.AlumnoId == alumno.Id && s.PlazaId == plazaId);
            if (yaAplicó)
                return BadRequest(new { error = "Ya aplicaste a esta plaza" });

            var solicitud = new SolicitudPractica
            {
                AlumnoId = alumno.Id,
                EmpresaId = plaza.EmpresaId,
                PlazaId = plaza.Id,
                Estado = "solicitado",
                FechaSolicitud = DateTimeOffset.UtcNow,
                Area = plaza.Area
            };

            _context.SolicitudesPracticas.Add(solicitud);
            await _context.SaveChangesAsync();

            return RedirectToAction("Peticiones");
        }

        // ── Confirmar entrevista (alumno) ─────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "alumno")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEntrevista(int entrevistaId)
        {
            var login = await GetLoginAsync();
            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.LoginId == login!.Id);

            var entrevista = await _context.Entrevistas
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.Id == entrevistaId && e.AlumnoId == alumno!.Id);

            if (entrevista == null) return NotFound();

            entrevista.ConfirmadoAlumno = true;
            entrevista.Estado = "confirmada";
            await _context.SaveChangesAsync();

            return RedirectToAction("Peticiones");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PORTAL EMPRESA
        // ══════════════════════════════════════════════════════════════════════

        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> Empresa()
        {
            var login = await GetLoginAsync();
            if (login == null) return RedirectToAction("Index", "Account");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login.Id);
            if (empresa == null) return RedirectToAction("Index", "Account");

            SetEmpresaViewData(empresa, "dashboard");

            var plazas = await _context.PlazasPracticas.Where(p => p.EmpresaId == empresa.Id).ToListAsync();
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
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);
            if (empresa == null) return RedirectToAction("Index", "Account");

            SetEmpresaViewData(empresa, "plazas");
            ViewBag.Empresa = empresa;
            ViewBag.Plazas = await _context.PlazasPracticas
                .Where(p => p.EmpresaId == empresa.Id)
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            return View("Empresa/Plazas");
        }

        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> EmpresaSolicitudes()
        {
            var login = await GetLoginAsync();
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);
            if (empresa == null) return RedirectToAction("Index", "Account");

            SetEmpresaViewData(empresa, "solicitudes");

            // Cargar solicitudes con documentos del alumno
            var solicitudes = await _context.SolicitudesPracticas
                .Include(s => s.Alumno).ThenInclude(a => a!.Documentos)
                .Include(s => s.Plaza)
                .Where(s => s.EmpresaId == empresa.Id)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            // Entrevistas ya agendadas por esta empresa
            var entrevistaIds = await _context.Entrevistas
                .Where(e => e.EmpresaId == empresa.Id)
                .Select(e => e.SolicitudId)
                .ToListAsync();

            ViewBag.Empresa = empresa;
            ViewBag.Solicitudes = solicitudes;
            ViewBag.EntrevistaIds = entrevistaIds;

            return View("Empresa/Solicitudes");
        }

        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> EmpresaDocumentos()
        {
            var login = await GetLoginAsync();
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);
            if (empresa == null) return RedirectToAction("Index", "Account");

            SetEmpresaViewData(empresa, "documentos");
            ViewBag.Empresa = empresa;
            ViewBag.Documentos = await _context.DocumentosEmpresa
                .Where(d => d.EmpresaId == empresa.Id)
                .OrderByDescending(d => d.FechaSubida)
                .ToListAsync();

            return View("Empresa/Documentos");
        }

        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> EmpresaPerfil()
        {
            var login = await GetLoginAsync();
            var empresa = await _context.Empresas
                .Include(e => e.Login)
                .FirstOrDefaultAsync(e => e.LoginId == login!.Id);
            if (empresa == null) return RedirectToAction("Index", "Account");

            SetEmpresaViewData(empresa, "perfil");
            ViewBag.Empresa = empresa;
            return View("Empresa/Perfil");
        }

        // ── Crear plaza ───────────────────────────────────────────────────────
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
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);
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

        // ── Cambiar estado plaza ──────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "empresa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoPlaza(int plazaId, string nuevoEstado)
        {
            var login = await GetLoginAsync();
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);
            var plaza = await _context.PlazasPracticas.FindAsync(plazaId);

            if (plaza == null || plaza.EmpresaId != empresa?.Id) return NotFound();

            var validos = new[] { "activa", "pausada", "cerrada", "borrador" };
            if (!validos.Contains(nuevoEstado)) return BadRequest();

            plaza.Estado = nuevoEstado;
            plaza.FechaActualizacion = DateTimeOffset.UtcNow;
            if (nuevoEstado == "activa" && plaza.FechaPublicacion == null)
                plaza.FechaPublicacion = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("EmpresaPlazas");
        }

        // ── Responder solicitud ───────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "empresa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResponderSolicitud(int solicitudId, string decision)
        {
            var login = await GetLoginAsync();
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);
            var sol = await _context.SolicitudesPracticas
                .Include(s => s.Plaza)
                .FirstOrDefaultAsync(s => s.Id == solicitudId);

            if (sol == null || sol.EmpresaId != empresa?.Id) return NotFound();

            if (decision == "aceptado")
            {
                sol.Estado = "aceptado";
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

        // ── Agendar entrevista (empresa agenda, alumno recibe notif) ──────────
        [HttpPost]
        [Authorize(Roles = "empresa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgendarEntrevista(
            int solicitudId, string fechaHora, string modalidad,
            string? ubicacionOLink, string? notas)
        {
            var login = await GetLoginAsync();
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);
            var sol = await _context.SolicitudesPracticas
                .Include(s => s.Alumno).ThenInclude(a => a!.Login)
                .Include(s => s.Plaza)
                .FirstOrDefaultAsync(s => s.Id == solicitudId);

            if (sol == null || sol.EmpresaId != empresa?.Id) return NotFound();

            var entrevista = new Entrevista
            {
                SolicitudId = solicitudId,
                AlumnoId = sol.AlumnoId,
                EmpresaId = empresa.Id,
                FechaHora = DateTimeOffset.Parse(fechaHora),
                Modalidad = modalidad,
                UbicacionOLink = ubicacionOLink,
                Notas = notas,
                Estado = "pendiente",
                ConfirmadoAlumno = false
            };

            _context.Entrevistas.Add(entrevista);
            await _context.SaveChangesAsync();

            // Notificar al alumno por email
            try
            {
                var correoAlumno = sol.Alumno?.Login?.CorreoInstitucional ?? "";
                if (!string.IsNullOrEmpty(correoAlumno))
                {
                    await _emailService.SendEntrevistaAgendadaAsync(
                        correoAlumno,
                        sol.Alumno?.NombreCompleto ?? "Alumno",
                        empresa.NombreEmpresa,
                        sol.Plaza?.Titulo ?? "Prácticas",
                        entrevista.FechaHora,
                        modalidad,
                        ubicacionOLink ?? "",
                        notas ?? "");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar email de entrevista para solicitud {Id}", solicitudId);
            }

            TempData["Mensaje"] = "✓ Entrevista agendada. El alumno fue notificado.";
            return RedirectToAction("EmpresaSolicitudes");
        }

        // ── Guardar perfil empresa ────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "empresa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPerfilEmpresa(
            string NombreEmpresa, string? RazonSocial, string? RFC,
            string? Ciudad, string? Sector, string? Giro, string? Direccion,
            string? NombreContacto, string? PuestoContacto, string? TelefonoContacto)
        {
            var login = await GetLoginAsync();
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);
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
            empresa.DatosCompletos =
                !string.IsNullOrEmpty(empresa.NombreEmpresa) &&
                !string.IsNullOrEmpty(empresa.RFC) &&
                !string.IsNullOrEmpty(empresa.NombreContacto);

            await _context.SaveChangesAsync();
            return RedirectToAction("EmpresaPerfil");
        }

        private void SetEmpresaViewData(Empresa empresa, string activeModule)
        {
            ViewData["UserName"] = empresa.NombreEmpresa;
            ViewData["UserRole"] = $"Empresa · {empresa.Sector ?? empresa.Giro ?? "Empresa"}";
            ViewData["ActiveModule"] = activeModule;
            ViewData["PortalTipo"] = "empresa";
        }
    }
}
