using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GMT.Models;

namespace GMT.Services
{
    public class VerificationService : IVerificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public VerificationService(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<RegistroPendiente?> ObtenerRegistroPendienteActivoAsync(Guid token)
        {
            var registro = await _context.RegistrosPendientes.FirstOrDefaultAsync(r => r.Id == token);
            if (registro == null) return null;
            if (registro.ExpiresAt < DateTime.UtcNow) return null;
            return registro;
        }

        public async Task<bool> ConfirmarRegistroAsync(Guid token)
        {
            var registro = await ObtenerRegistroPendienteActivoAsync(token);
            if (registro == null) return false;

            try
            {
                using var tx = await _context.Database.BeginTransactionAsync();

                // Deserializar datos del preregistro
                using var doc = JsonDocument.Parse(registro.DatosJson);
                var root = doc.RootElement;

                var correo = root.GetProperty("CorreoElectronico").GetString() ?? throw new InvalidOperationException("Correo faltante");
                var passwordHash = root.GetProperty("PasswordHash").GetString() ?? throw new InvalidOperationException("PasswordHash faltante");
                var tipo = root.TryGetProperty("TipoRegistro", out var t) ? t.GetString() ?? "Empresa" : "Empresa";

                // Crear Login
                var login = new Login
                {
                    CorreoInstitucional = correo,
                    PasswordHash = passwordHash,
                    IntentosFallidos = 0,
                    UltimoAcceso = null,
                    EstaVerificado = true
                };
                _context.Logins.Add(login);
                await _context.SaveChangesAsync();

                if (string.Equals(tipo, "Empresa", StringComparison.OrdinalIgnoreCase))
                {
                    var empresa = new Empresa
                    {
                        NombreEmpresa = root.TryGetProperty("NombreEmpresa", out var ne) ? ne.GetString() : null,
                        RFC = root.TryGetProperty("RFC", out var rfc) ? rfc.GetString() : null,
                        CorreoElectronico = correo,
                        PasswordHash = passwordHash,
                        EstaVerificado = true,
                        FechaRegistro = DateTime.UtcNow,
                        LoginId = login.Id
                    };
                    _context.Empresas.Add(empresa);
                    await _context.SaveChangesAsync();
                }
                else if (string.Equals(tipo, "Alumno", StringComparison.OrdinalIgnoreCase))
                {
                    // Insertar en la tabla Alumnos
                    var alumno = new Alumno
                    {
                        LoginId = login.Id,
                        NombreCompleto = root.TryGetProperty("NombreCompleto", out var nc) ? nc.GetString() : null,
                        Semestre = root.TryGetProperty("Semestre", out var sem) && sem.TryGetInt32(out var semestre) ? semestre : 0,
                        NumeroControl = root.TryGetProperty("Matricula", out var mat) ? mat.GetString() : null,
                        Carrera = root.TryGetProperty("Carrera", out var car) ? car.GetString() : null
                    };
                    _context.Alumnos.Add(alumno);
                    await _context.SaveChangesAsync();
                }

                // Eliminar preregistro
                _context.RegistrosPendientes.Remove(registro);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                // Envío de bienvenida (no crítico para la transacción)
                try
                {
                    // Intenta obtener el nombre del alumno, si no el de la empresa, y por último el correo
                    var displayName = root.TryGetProperty("NombreCompleto", out var n) ? n.GetString()
                                      : (root.TryGetProperty("NombreEmpresa", out var ne) ? ne.GetString() : null);

                    // Si displayName es null, usa el correo como valor predeterminado para evitar null
                    await _emailService.SendWelcomeEmailAsync(correo, displayName ?? correo);
                }
                catch
                {
                    // Log si lo necesitas; no cancelamos la promoción por fallo del welcome mail
                }

                return true;
            }
            catch
            {
                // Aquí podrías agregar un _logger.LogError(ex, "Mensaje") si inyectas ILogger
                return false;
            }
        }
    }
}