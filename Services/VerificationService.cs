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
                    UltimoAcceso = null
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
                else
                {
                    // TODO: crear alumno si aplica (usar modelo Alumno)
                }

                // Eliminar preregistro
                _context.RegistrosPendientes.Remove(registro);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                // Envío de bienvenida (no crítico para la transacción)
                try
                {
                    var displayName = root.TryGetProperty("NombreEmpresa", out var n) ? n.GetString() ?? correo : correo;
                    await _emailService.SendWelcomeEmailAsync(correo, displayName);
                }
                catch
                {
                    // Log si lo necesitas; no cancelamos la promoción por fallo del welcome mail
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}