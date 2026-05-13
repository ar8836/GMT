using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GMT.Data;
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
            if (registro.ExpiresAt < DateTimeOffset.UtcNow) return null;
            return registro;
        }

        public async Task<bool> ConfirmarRegistroAsync(Guid token)
        {
            var registro = await ObtenerRegistroPendienteActivoAsync(token);
            if (registro == null) return false;

            try
            {
                using var tx = await _context.Database.BeginTransactionAsync();

                using var doc = JsonDocument.Parse(registro.DatosJson);
                var root = doc.RootElement;

                var correo = root.GetProperty("CorreoElectronico").GetString() ?? throw new InvalidOperationException("Correo faltante");
                var passwordHash = root.GetProperty("PasswordHash").GetString() ?? throw new InvalidOperationException("PasswordHash faltante");
                var tipo = root.TryGetProperty("TipoRegistro", out var t) ? t.GetString() ?? "Alumno" : "Alumno";

                // ── Crear Login ───────────────────────────────────────────────
                var login = new Login
                {
                    CorreoInstitucional = correo,
                    PasswordHash = passwordHash,
                    Rol = tipo.Equals("Empresa", StringComparison.OrdinalIgnoreCase) ? "empresa" : "alumno",
                    IntentosFallidos = 0,
                    UltimoAcceso = null,
                    EsVerificado = true
                };
                _context.Logins.Add(login);
                await _context.SaveChangesAsync();

                // ── Empresa ───────────────────────────────────────────────────
                if (tipo.Equals("Empresa", StringComparison.OrdinalIgnoreCase))
                {
                    var empresa = new Empresa
                    {
                        LoginId = login.Id,
                        NombreEmpresa = root.TryGetProperty("NombreEmpresa", out var ne) ? ne.GetString() ?? string.Empty : string.Empty,
                        RazonSocial = root.TryGetProperty("RazonSocial", out var rs) ? rs.GetString() : null,
                        RFC = root.TryGetProperty("RFC", out var rfc) ? rfc.GetString() ?? string.Empty : string.Empty,
                        Sector = root.TryGetProperty("Sector", out var sec) ? sec.GetString() : null,
                        Giro = root.TryGetProperty("Giro", out var giro) ? giro.GetString() : null,
                        Ciudad = root.TryGetProperty("Ciudad", out var ciu) ? ciu.GetString() : null,
                        NombreContacto = root.TryGetProperty("NombreContacto", out var nc) ? nc.GetString() : null,
                        PuestoContacto = root.TryGetProperty("PuestoContacto", out var pc) ? pc.GetString() : null,
                        TelefonoContacto = root.TryGetProperty("TelefonoContacto", out var tc) ? tc.GetString() : null,
                        EstaVerificado = true,
                        FechaRegistro = DateTimeOffset.UtcNow
                    };
                    _context.Empresas.Add(empresa);
                    await _context.SaveChangesAsync();
                }
                // ── Alumno ────────────────────────────────────────────────────
                else if (tipo.Equals("Alumno", StringComparison.OrdinalIgnoreCase))
                {
                    var alumno = new Alumno
                    {
                        LoginId = login.Id,
                        NombreCompleto = root.TryGetProperty("NombreCompleto", out var nom) ? nom.GetString() : null,
                        NumeroControl = root.TryGetProperty("Matricula", out var mat) ? mat.GetString() : null,
                        Carrera = root.TryGetProperty("Carrera", out var car) ? car.GetString() : null,
                        Semestre = root.TryGetProperty("Semestre", out var sem) && sem.TryGetInt32(out var s) ? s : null,
                        Telefono = root.TryGetProperty("Telefono", out var tel) ? tel.GetString() : null,
                        DatosCompletos = false
                    };
                    _context.Alumnos.Add(alumno);
                    await _context.SaveChangesAsync();
                }

                // ── Limpiar preregistro ───────────────────────────────────────
                _context.RegistrosPendientes.Remove(registro);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // ── Email de bienvenida (no crítico) ──────────────────────────
                try
                {
                    var displayName = root.TryGetProperty("NombreCompleto", out var dn) ? dn.GetString()
                                    : root.TryGetProperty("NombreEmpresa", out var den) ? den.GetString() : null;
                    await _emailService.SendWelcomeEmailAsync(correo, displayName ?? correo);
                }
                catch { /* No cancelamos la transacción por fallo de email */ }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
