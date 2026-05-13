using GMT.Data;
using GMT.Models;
using GMT.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
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

        [HttpPut("{id}/photo")]
        public async Task<IActionResult> UpdateAlumnosPhoto(int id, [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file provided");

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

                string key = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
                string uploadedKey = await _s3Service.UploadFileAsync(file, key);
                string fileUrl = await _s3Service.GetFileUrlAsync(uploadedKey, 24);

                alumno.FotoPerfilUrl = fileUrl;
                await _context.SaveChangesAsync();

                string? studentEmail = alumno.Login?.CorreoInstitucional;
                if (!string.IsNullOrEmpty(studentEmail))
                {
                    await _emailService.SendWelcomeEmailAsync(
                        studentEmail,
                        alumno.NombreCompleto ?? "Estudiante");
                }

                return Ok(new { message = "Foto de perfil actualizada exitosamente", url = fileUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[UploadController] ERROR: {ex.GetType().Name} — {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"  Inner: {ex.InnerException.Message}");

                return StatusCode(500, new
                {
                    error = "Internal server error",
                    details = ex.Message,
                    type = ex.GetType().Name
                });
            }
        }
    }
}
