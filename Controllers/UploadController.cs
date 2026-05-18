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

        // ── Foto de perfil alumno ─────────────────────────────────────────────
        [HttpPut("{id}/photo")]
        [Authorize(Roles = "alumno")]
        public async Task<IActionResult> UpdateAlumnosPhoto(int id, [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest("No file provided");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Only JPG, JPEG and PNG files are allowed");

                if (file.Length > 2 * 1024 * 1024)
                    return BadRequest("File size exceeds 2MB limit");

                var alumno = await _context.Alumnos
                    .Include(a => a.Login)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (alumno == null)
                    return NotFound($"Alumno with ID {id} not found");

                string key = $"fotos/{Guid.NewGuid():N}{extension}";
                string uploadedKey = await _s3Service.UploadFileAsync(file, key);
                string fileUrl = await _s3Service.GetFileUrlAsync(uploadedKey, 24);

                alumno.FotoPerfilUrl = fileUrl;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Foto de perfil actualizada exitosamente", url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        // ── Documento empresa (convenio, acta, etc.) ─────────────────────────
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

                // Validar extensión — solo PDF para documentos empresa
                var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if (ext != ".pdf")
                    return BadRequest(new { error = "Solo se aceptan archivos PDF" });

                // Validar tamaño (10 MB)
                if (archivo.Length > 10 * 1024 * 1024)
                    return BadRequest(new { error = "El archivo supera los 10 MB permitidos" });

                // Validar tipo
                var tiposValidos = new[] { "convenio", "acta_constitutiva", "comprobante_domicilio", "otro" };
                if (!tiposValidos.Contains(tipoDocumento))
                    return BadRequest(new { error = "Tipo de documento no válido" });

                // Obtener empresa del usuario autenticado
                var correo = User.FindFirstValue(ClaimTypes.Name);
                var login = await _context.Logins.FirstOrDefaultAsync(l => l.CorreoInstitucional == correo);
                var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.LoginId == login!.Id);

                if (empresa == null)
                    return Unauthorized(new { error = "Empresa no encontrada" });

                // Subir a S3
                string s3Key = $"empresas/{empresa.Id}/docs/{Guid.NewGuid():N}{ext}";
                string uploadedKey = await _s3Service.UploadFileAsync(archivo, s3Key);
                string url = await _s3Service.GetFileUrlAsync(uploadedKey, 24 * 365); // URL larga vigencia

                // Registrar en DB
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
