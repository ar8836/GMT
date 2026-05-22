using GMT.Data;
using GMT.Models;
using GMT.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GMT.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly S3Service _s3Service;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            EmailService emailService,
            S3Service s3Service,
            ILogger<AdminController> logger)
        {
            _context = context;
            _emailService = emailService;
            _s3Service = s3Service;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GET /Admin  —  Panel de Administrador (resumen)
        // ══════════════════════════════════════════════════════════════════════
        public async Task<IActionResult> Index()
        {
            await SetAdminViewDataAsync("dashboard");

            ViewBag.TotalEmpresas = await _context.Empresas.CountAsync();
            ViewBag.EmpresasPendientes = await _context.Empresas.CountAsync(e => !e.EstaVerificado);
            ViewBag.EmpresasVerificadas = await _context.Empresas.CountAsync(e => e.EstaVerificado);
            ViewBag.TotalAlumnos = await _context.Alumnos.CountAsync();
            ViewBag.DocsPendientesTotal = await _context.DocumentosEmpresa
                                               .CountAsync(d => d.Estado == "pendiente");
            ViewBag.EmpresasBloqueadasN = await _context.ListaNegraEmpresas
                                               .CountAsync(l => l.Activo);

            // Últimas 5 empresas registradas pendientes de revisión
            ViewBag.UltimasPendientes = await _context.Empresas
                .Include(e => e.DocumentosEmpresa)
                .Where(e => !e.EstaVerificado)
                .OrderByDescending(e => e.FechaRegistro)
                .Take(5)
                .ToListAsync();

            return View("~/Views/Dashboard/Admin/Index.cshtml");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GET /Admin/Documentos  —  Revisión de documentos de empresas
        //  Este es el centro del panel: el admin ve los docs subidos por
        //  las empresas y los aprueba o rechaza. La aprobación de todos los
        //  docs obligatorios desbloquea a la empresa para publicar plazas.
        // ══════════════════════════════════════════════════════════════════════
        public async Task<IActionResult> Documentos(string? filtro = "pendientes")
        {
            await SetAdminViewDataAsync("documentos");

            // Cargar documentos agrupados por empresa
            var docsQuery = _context.DocumentosEmpresa
                .Include(d => d.Empresa)
                    .ThenInclude(e => e!.Login)
                .AsQueryable();

            if (filtro == "pendientes")
                docsQuery = docsQuery.Where(d => d.Estado == "pendiente");
            else if (filtro == "aprobados")
                docsQuery = docsQuery.Where(d => d.Estado == "aprobado");
            else if (filtro == "rechazados")
                docsQuery = docsQuery.Where(d => d.Estado == "rechazado");

            var docs = await docsQuery
                .OrderByDescending(d => d.FechaSubida)
                .ToListAsync();

            ViewBag.Documentos = docs;
            ViewBag.Filtro = filtro;
            ViewBag.CountPendientes = await _context.DocumentosEmpresa.CountAsync(d => d.Estado == "pendiente");
            ViewBag.CountAprobados = await _context.DocumentosEmpresa.CountAsync(d => d.Estado == "aprobado");
            ViewBag.CountRechazados = await _context.DocumentosEmpresa.CountAsync(d => d.Estado == "rechazado");

            return View("~/Views/Dashboard/Admin/Documentos.cshtml");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /Admin/AprobarDocumento
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarDocumento(int documentoId)
        {
            var doc = await _context.DocumentosEmpresa
                .Include(d => d.Empresa)
                    .ThenInclude(e => e!.Login)
                .FirstOrDefaultAsync(d => d.Id == documentoId);

            if (doc == null) return NotFound();

            doc.Estado = "aprobado";
            await _context.SaveChangesAsync();

            // Verificar si la empresa ahora tiene todos los docs obligatorios aprobados
            await EvaluarVerificacionEmpresa(doc.EmpresaId);

            _logger.LogInformation("Documento {Id} de empresa {EmpId} aprobado por {Admin}",
                documentoId, doc.EmpresaId, User.FindFirstValue(ClaimTypes.Name));

            TempData["Mensaje"] = $"✓ Documento aprobado correctamente.";
            return RedirectToAction(nameof(Documentos));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /Admin/RechazarDocumento
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarDocumento(int documentoId, string motivo)
        {
            var doc = await _context.DocumentosEmpresa
                .Include(d => d.Empresa)
                    .ThenInclude(e => e!.Login)
                .FirstOrDefaultAsync(d => d.Id == documentoId);

            if (doc == null) return NotFound();

            doc.Estado = "rechazado";
            await _context.SaveChangesAsync();

            // Notificar a la empresa para que corrija el documento
            try
            {
                await _emailService.SendDocumentoRechazadoAsync(
                    doc.Empresa?.Login?.CorreoInstitucional ?? "",
                    doc.Empresa?.NombreEmpresa ?? "",
                    doc.Empresa?.NombreContacto ?? "",
                    doc.NombreArchivo,
                    motivo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enviando email rechazo doc {Id}", documentoId);
            }

            TempData["Mensaje"] = "Documento rechazado. La empresa fue notificada.";
            return RedirectToAction(nameof(Documentos));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GET /Admin/Empresas  —  Vista de empresas aprobadas (tab)
        // ══════════════════════════════════════════════════════════════════════
        public async Task<IActionResult> Empresas(string? filtro = "verificadas")
        {
            await SetAdminViewDataAsync("aprobadas");

            var query = _context.Empresas
                .Include(e => e.Login)
                .Include(e => e.DocumentosEmpresa)
                .AsQueryable();

            if (filtro == "pendientes")
                query = query.Where(e => !e.EstaVerificado);
            else if (filtro == "verificadas")
                query = query.Where(e => e.EstaVerificado);

            ViewBag.Empresas = await query
                .OrderByDescending(e => e.FechaRegistro)
                .ToListAsync();
            ViewBag.Filtro = filtro;

            return View("~/Views/Dashboard/Admin/Empresas.cshtml");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /Admin/AprobarEmpresa  (acceso manual además del auto)
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarEmpresa(int empresaId, string? notas)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Login)
                .FirstOrDefaultAsync(e => e.Id == empresaId);

            if (empresa == null) return NotFound();

            var adminCorreo = User.FindFirstValue(ClaimTypes.Name) ?? "admin";
            empresa.EstaVerificado = true;
            empresa.DatosCompletos = true;

            _context.VerificacionesEmpresa.Add(new VerificacionEmpresa
            {
                EmpresaId = empresaId,
                Decision = "aprobada",
                Motivo = notas ?? "Verificación manual",
                RevisadoPor = adminCorreo,
                FechaRevision = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendEmpresaAprobadaAsync(
                    empresa.Login?.CorreoInstitucional ?? "",
                    empresa.NombreEmpresa,
                    empresa.NombreContacto ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enviando email aprobación empresa {Id}", empresaId);
            }

            TempData["Mensaje"] = $"✓ {empresa.NombreEmpresa} verificada correctamente.";
            return RedirectToAction(nameof(Empresas));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /Admin/RechazarEmpresa
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarEmpresa(int empresaId, string motivo)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Login)
                .FirstOrDefaultAsync(e => e.Id == empresaId);

            if (empresa == null) return NotFound();

            var adminCorreo = User.FindFirstValue(ClaimTypes.Name) ?? "admin";
            empresa.EstaVerificado = false;

            _context.VerificacionesEmpresa.Add(new VerificacionEmpresa
            {
                EmpresaId = empresaId,
                Decision = "rechazada",
                Motivo = motivo,
                RevisadoPor = adminCorreo,
                FechaRevision = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendEmpresaRechazadaAsync(
                    empresa.Login?.CorreoInstitucional ?? "",
                    empresa.NombreEmpresa,
                    empresa.NombreContacto ?? "",
                    motivo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enviando email rechazo empresa {Id}", empresaId);
            }

            TempData["Mensaje"] = $"Empresa {empresa.NombreEmpresa} rechazada.";
            return RedirectToAction(nameof(Empresas));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GET /Admin/ListaNegra
        // ══════════════════════════════════════════════════════════════════════
        public async Task<IActionResult> ListaNegra()
        {
            await SetAdminViewDataAsync("lista-negra");

            var listaNegra = await _context.ListaNegraEmpresas
                .Include(l => l.Empresa)
                    .ThenInclude(e => e!.Login)
                .OrderByDescending(l => l.FechaIngreso)
                .ToListAsync();

            ViewBag.ListaNegra = listaNegra;

            return View("~/Views/Dashboard/Admin/ListaNegra.cshtml");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /Admin/AgregarListaNegra
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarListaNegra(int empresaId, string motivo)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Login)
                .FirstOrDefaultAsync(e => e.Id == empresaId);

            if (empresa == null) return NotFound();

            var adminCorreo = User.FindFirstValue(ClaimTypes.Name) ?? "admin";

            // Verificar si ya está en lista negra activa
            var yaEnLista = await _context.ListaNegraEmpresas
                .FirstOrDefaultAsync(l => l.EmpresaId == empresaId);

            if (yaEnLista != null)
            {
                yaEnLista.Activo = true;
                yaEnLista.Motivo = motivo;
                yaEnLista.AgregadoPor = adminCorreo;
                yaEnLista.FechaIngreso = DateTimeOffset.UtcNow;
            }
            else
            {
                _context.ListaNegraEmpresas.Add(new ListaNegraEmpresa
                {
                    EmpresaId = empresaId,
                    Motivo = motivo,
                    AgregadoPor = adminCorreo,
                    FechaIngreso = DateTimeOffset.UtcNow,
                    Activo = true
                });
            }

            // Revocar verificación
            empresa.EstaVerificado = false;
            await _context.SaveChangesAsync();

            _logger.LogWarning("Empresa {Id} ({Nombre}) agregada a lista negra por {Admin}. Motivo: {Motivo}",
                empresaId, empresa.NombreEmpresa, adminCorreo, motivo);

            TempData["Mensaje"] = $"Empresa {empresa.NombreEmpresa} agregada a lista negra.";
            return RedirectToAction(nameof(ListaNegra));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /Admin/LevantarListaNegra
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LevantarListaNegra(int listaNegraId)
        {
            var entrada = await _context.ListaNegraEmpresas
                .Include(l => l.Empresa)
                .FirstOrDefaultAsync(l => l.Id == listaNegraId);

            if (entrada == null) return NotFound();

            entrada.Activo = false;
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Bloqueo levantado para {entrada.Empresa?.NombreEmpresa}.";
            return RedirectToAction(nameof(ListaNegra));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GET /Admin/VerDocumento/{id}  —  Genera URL temporal de S3
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> VerDocumento(int id)
        {
            var doc = await _context.DocumentosEmpresa.FindAsync(id);
            if (doc == null) return NotFound();

            try
            {
                // URL pre-firmada válida por 15 minutos para revisar el PDF
                var url = await _s3Service.GetFileUrlAsync(doc.S3Key, 0);
                return Redirect(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando URL pre-firmada para doc {Id}", id);
                TempData["Error"] = "No se pudo abrir el documento. Inténtalo de nuevo.";
                return RedirectToAction(nameof(Documentos));
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPER PRIVADO — SetAdminViewDataAsync
        //  Carga los contadores de badges del sidebar en cada petición.
        // ══════════════════════════════════════════════════════════════════════
        private async Task SetAdminViewDataAsync(string activeModule)
        {
            ViewData["ActiveModule"] = activeModule;
            ViewData["PortalTipo"] = "admin";
            ViewData["UserName"] = "Vinculación TecNM";
            ViewData["UserRole"] = "Administrador";

            // Badges del sidebar
            ViewBag.DocsPendientes = await _context.DocumentosEmpresa
                                              .CountAsync(d => d.Estado == "pendiente");
            ViewBag.EmpresasBloqueadas = await _context.ListaNegraEmpresas
                                              .CountAsync(l => l.Activo);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPER PRIVADO — EvaluarVerificacionEmpresa
        //  Aprueba automáticamente a la empresa si tiene todos los docs
        //  obligatorios (convenio + acta) aprobados.
        // ══════════════════════════════════════════════════════════════════════
        private async Task EvaluarVerificacionEmpresa(int empresaId)
        {
            var docs = await _context.DocumentosEmpresa
                .Where(d => d.EmpresaId == empresaId)
                .ToListAsync();

            bool tieneConvenio = docs.Any(d => d.TipoDocumento == "convenio" && d.Estado == "aprobado");
            bool tieneActa = docs.Any(d => d.TipoDocumento == "acta_constitutiva" && d.Estado == "aprobado");

            if (!tieneConvenio || !tieneActa) return;

            var empresa = await _context.Empresas
                .Include(e => e.Login)
                .FirstOrDefaultAsync(e => e.Id == empresaId);

            if (empresa == null || empresa.EstaVerificado) return;

            // Verificación automática por documentos completos
            empresa.EstaVerificado = true;
            empresa.DatosCompletos = true;

            _context.VerificacionesEmpresa.Add(new VerificacionEmpresa
            {
                EmpresaId = empresaId,
                Decision = "aprobada",
                Motivo = "Aprobación automática: convenio y acta constitutiva aprobados",
                RevisadoPor = "sistema",
                FechaRevision = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notificar a la empresa
            try
            {
                await _emailService.SendEmpresaAprobadaAsync(
                    empresa.Login?.CorreoInstitucional ?? "",
                    empresa.NombreEmpresa,
                    empresa.NombreContacto ?? "");
            }
            catch { }

            _logger.LogInformation("Empresa {Id} ({Nombre}) aprobada automáticamente por documentos completos",
                empresaId, empresa.NombreEmpresa);
        }
    }
}
