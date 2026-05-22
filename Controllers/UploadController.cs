using GMT.Data;
using GMT.Models;
using GMT.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GMT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly S3Service _s3Service;
        private readonly EmailService _emailService;

        public UploadController(ApplicationDbContext context, S3Service s3Service, EmailService emailService)
        {
            _context = context;
            _s3Service = s3Service;
            _emailService = emailService;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FOTO DE PERFIL — ALUMNO
        //  PUT /api/Upload/{id}/photo
        // ══════════════════════════════════════════════════════════════════════
        [HttpPut("{id}/photo")]
        [Authorize(Roles = "alumno")]
        public async Task<IActionResult> UpdateAlumnoPhoto(int id, [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "No file provided" });

                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                    return BadRequest(new { error = "Solo se aceptan JPG, PNG o WEBP" });

                if (file.Length > 3 * 1024 * 1024)
                    return BadRequest(new { error = "La imagen supera los 3 MB" });

                // Verificar que el alumno pertenezca al usuario autenticado
                var correo = User.FindFirstValue(ClaimTypes.Name);
                var login = await _context.Logins.FirstOrDefaultAsync(l => l.CorreoInstitucional == correo);
                var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.Id == id && a.LoginId == login!.Id);

                if (alumno == null)
                    return Forbid();

                string key = $"fotos/alumnos/{alumno.Id}/{Guid.NewGuid():N}{ext}";
                string uploadedKey = await _s3Service.UploadFileAsync(file, key);
                string fileUrl = await _s3Service.GetFileUrlAsync(uploadedKey, 24 * 365);

                alumno.FotoPerfilUrl = fileUrl;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Foto actualizada", url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno", details = ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FOTO DE PERFIL — EMPRESA
        //  PUT /api/Upload/empresa/photo
        // ══════════════════════════════════════════════════════════════════════
        [HttpPut("empresa/photo")]
        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> UpdateEmpresaPhoto([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "No file provided" });

                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                    return BadRequest(new { error = "Solo se aceptan JPG, PNG o WEBP" });

                if (file.Length > 3 * 1024 * 1024)
                    return BadRequest(new { error = "La imagen supera los 3 MB" });

                var correo = User.FindFirstValue(ClaimTypes.Name);
                var login = await _context.Logins.FirstOrDefaultAsync(l => l.CorreoInstitucional == correo);
                var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);

                if (empresa == null)
                    return Forbid();

                string key = $"fotos/empresas/{empresa.Id}/{Guid.NewGuid():N}{ext}";
                string uploadedKey = await _s3Service.UploadFileAsync(file, key);
                string fileUrl = await _s3Service.GetFileUrlAsync(uploadedKey, 24 * 365);

                empresa.FotoPerfilUrl = fileUrl;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Logo/foto actualizada", url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno", details = ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DOCUMENTO ALUMNO (CV o Constancia)
        //  POST /api/Upload/{id}/document
        //  Body: file (PDF), tipoDocumento (cv | constancia_estudios)
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("{id}/document")]
        [Authorize(Roles = "alumno")]
        public async Task<IActionResult> SubirDocumentoAlumno(
            int id,
            [FromForm] IFormFile file,
            [FromForm] string tipoDocumento)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "No se proporcionó archivo" });

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".pdf")
                    return BadRequest(new { error = "Solo se aceptan archivos PDF" });

                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(new { error = "El archivo supera los 5 MB" });

                // Solo CV y constancia de estudios
                var tiposValidos = new[] { "cv", "constancia_estudios" };
                if (!tiposValidos.Contains(tipoDocumento))
                    return BadRequest(new { error = "Tipo de documento no válido. Use: cv o constancia_estudios" });

                // Verificar que el alumno pertenezca al usuario autenticado
                var correo = User.FindFirstValue(ClaimTypes.Name);
                var login = await _context.Logins.FirstOrDefaultAsync(l => l.CorreoInstitucional == correo);
                var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.Id == id && a.LoginId == login!.Id);

                if (alumno == null)
                    return Forbid();

                string s3Key = $"alumnos/{alumno.Id}/docs/{tipoDocumento}/{Guid.NewGuid():N}{ext}";
                string uploadedKey = await _s3Service.UploadFileAsync(file, s3Key);
                string url = await _s3Service.GetFileUrlAsync(uploadedKey, 24 * 365);

                var doc = new DocumentoAlumno
                {
                    AlumnoId = alumno.Id,
                    NombreArchivo = file.FileName,
                    S3Key = uploadedKey,
                    Url = url,
                    TamañoBytes = file.Length,
                    TipoDocumento = tipoDocumento,   // 'cv' | 'constancia_estudios'
                    Estado = "pendiente",
                    FechaSubida = DateTimeOffset.UtcNow
                };

                _context.DocumentosAlumno.Add(doc);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Documento subido correctamente",
                    documentoId = doc.Id,
                    url,
                    tipoDocumento,
                    nombreArchivo = file.FileName,
                    tamano = file.Length
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno", details = ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DOCUMENTO EMPRESA (convenio, acta, etc.)
        //  POST /api/Upload/empresa-documento
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("empresa-documento")]
        [Authorize(Roles = "empresa")]
        public async Task<IActionResult> SubirDocumentoEmpresa(
            [FromForm] IFormFile archivo,
            [FromForm] string tipoDocumento)
        {
            try
            {
                if (archivo == null || archivo.Length == 0)
                    return BadRequest(new { error = "No se proporcionó archivo" });

                var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if (ext != ".pdf")
                    return BadRequest(new { error = "Solo se aceptan archivos PDF" });

                if (archivo.Length > 10 * 1024 * 1024)
                    return BadRequest(new { error = "El archivo supera los 10 MB" });

                var tiposValidos = new[] { "convenio", "acta_constitutiva", "comprobante_domicilio", "otro" };
                if (!tiposValidos.Contains(tipoDocumento))
                    return BadRequest(new { error = "Tipo de documento no válido" });

                var correo = User.FindFirstValue(ClaimTypes.Name);
                var login = await _context.Logins.FirstOrDefaultAsync(l => l.CorreoInstitucional == correo);
                var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);

                if (empresa == null)
                    return Unauthorized(new { error = "Empresa no encontrada" });

                string s3Key = $"empresas/{empresa.Id}/docs/{Guid.NewGuid():N}{ext}";
                string uploadedKey = await _s3Service.UploadFileAsync(archivo, s3Key);
                string url = await _s3Service.GetFileUrlAsync(uploadedKey, 24 * 365);

                var doc = new DocumentoEmpresa
                {
                    EmpresaId = empresa.Id,
                    NombreArchivo = archivo.FileName,
                    S3Key = uploadedKey,
                    Url = url,
                    TamanoBytes = archivo.Length,
                    TipoDocumento = tipoDocumento,
                    Estado = "pendiente"
                };

                _context.DocumentosEmpresa.Add(doc);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Documento subido correctamente", documentoId = doc.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno", details = ex.Message });
            }
        }
    }
}
