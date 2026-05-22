using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace GMT.Services
{
    public class EmailService
    {
        private IAmazonSimpleEmailService? _sesClient;
        private readonly string _fromAddress = "no-reply@gmtek.lol";
        private readonly string _correoVinculacion;
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _sesClient = null;
            _correoVinculacion = configuration["Vinculacion:CorreoRevision"]
                                 ?? "vinculacion@gmtek.lol";
        }

        // ── Infraestructura SES ───────────────────────────────────────────────

        private IAmazonSimpleEmailService GetSesClient()
        {
            if (_sesClient != null) return _sesClient;

            var accessKey = _configuration["AWS:AccessKey"];
            var secretKey = _configuration["AWS:SecretKey"];
            var region = _configuration["AWS:Region"] ?? "us-east-1";

            Amazon.Runtime.AWSCredentials credentials =
                !string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey)
                    ? new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey)
                    : Amazon.Runtime.FallbackCredentialsFactory.GetCredentials();

            _sesClient = new AmazonSimpleEmailServiceClient(
                credentials,
                Amazon.RegionEndpoint.GetBySystemName(region));

            return _sesClient;
        }

        /// <summary>Envía un email HTML genérico.</summary>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("No email provided", nameof(toEmail));

            var request = new SendEmailRequest
            {
                Source = _fromAddress,
                Destination = new Destination { ToAddresses = { toEmail } },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body { Html = new Content(htmlBody) }
                }
            };

            await GetSesClient().SendEmailAsync(request);
        }

        // ── Métodos de negocio ────────────────────────────────────────────────

        /// <summary>Bienvenida al nuevo usuario (alumno o empresa).</summary>
        public async Task SendWelcomeEmailAsync(string toEmail, string displayName)
        {
            var html = $@"
                <div style='font-family:Arial,sans-serif;font-size:14px;color:#333;'>
                    <h2 style='color:#002366;'>Bienvenido a GMT</h2>
                    <p>Hola <strong>{displayName}</strong>,</p>
                    <p>Tu cuenta en <a href='https://gmtek.lol'>GMT — Gestor Modular Tecnológico</a> está lista.</p>
                    <p style='color:#666;font-size:12px;'>TecNM Campus Acapulco</p>
                </div>";

            await SendEmailAsync(toEmail, "Bienvenido a GMT", html);
        }

        /// <summary>Correo de verificación de cuenta.</summary>
        public async Task SendVerificationEmailAsync(string toEmail, string verificationLink)
        {
            var html = $@"
                <div style='font-family:Arial,sans-serif;font-size:14px;color:#333;'>
                    <h2 style='color:#002366;'>Verificación de cuenta GMT</h2>
                    <p>Gracias por registrarte. Haz clic en el botón para activar tu cuenta:</p>
                    <p>
                        <a href='{verificationLink}'
                           style='background:#3498db;color:#fff;padding:10px 22px;
                                  text-decoration:none;border-radius:6px;display:inline-block;'>
                            Activar cuenta
                        </a>
                    </p>
                    <p>O copia este enlace:<br/><small>{verificationLink}</small></p>
                    <p><small>Este enlace expira en 24 horas.</small></p>
                </div>";

            await SendEmailAsync(toEmail, "Verificación de cuenta — GMT", html);
        }

        /// <summary>
        /// Notifica a Vinculación y a la empresa cuando ésta confirma su email
        /// y queda pendiente de revisión manual.
        /// </summary>
        public async Task SendEmpresaPendienteRevisionAsync(
            string correoEmpresa, string nombreEmpresa,
            string rfc, string nombreContacto, string telefonoContacto)
        {
            // Email a Vinculación
            var htmlVinculacion = $@"
                <div style='font-family:Arial,sans-serif;font-size:14px;color:#333;'>
                    <h2 style='color:#002366;'>Nueva empresa pendiente de verificación</h2>
                    <table style='border-collapse:collapse;width:100%;'>
                        <tr><td style='padding:8px;font-weight:bold;width:180px;'>Empresa</td><td style='padding:8px;'>{nombreEmpresa}</td></tr>
                        <tr style='background:#f8f9fa;'><td style='padding:8px;font-weight:bold;'>RFC</td><td style='padding:8px;font-family:monospace;'>{rfc}</td></tr>
                        <tr><td style='padding:8px;font-weight:bold;'>Contacto</td><td style='padding:8px;'>{nombreContacto}</td></tr>
                        <tr style='background:#f8f9fa;'><td style='padding:8px;font-weight:bold;'>Teléfono</td><td style='padding:8px;'>{telefonoContacto}</td></tr>
                        <tr><td style='padding:8px;font-weight:bold;'>Correo</td><td style='padding:8px;'>{correoEmpresa}</td></tr>
                    </table>
                    <p style='margin-top:20px;'>
                        <a href='https://gmtek.lol/Admin/Documentos'
                           style='background:#002366;color:#fff;padding:10px 22px;text-decoration:none;border-radius:6px;display:inline-block;'>
                            Ir a Revisión de Documentos
                        </a>
                    </p>
                </div>";

            await SendEmailAsync(_correoVinculacion,
                $"GMT — Nueva empresa pendiente: {nombreEmpresa}", htmlVinculacion);

            // Email a la empresa
            var htmlEmpresa = $@"
                <div style='font-family:Arial,sans-serif;font-size:14px;color:#333;'>
                    <h2 style='color:#7c3aed;'>Tu registro está en revisión</h2>
                    <p>Hola <strong>{nombreContacto}</strong>,</p>
                    <p>Tu cuenta para <strong>{nombreEmpresa}</strong> fue creada exitosamente en GMT.</p>
                    <p>Para poder publicar plazas, el departamento de <strong>Vinculación del TecNM Campus Acapulco</strong>
                       revisará tus documentos. Sube tu convenio y acta constitutiva desde tu portal para agilizar el proceso.</p>
                    <p>
                        <a href='https://gmtek.lol/Dashboard/EmpresaDocumentos'
                           style='background:#7c3aed;color:#fff;padding:10px 22px;text-decoration:none;border-radius:6px;display:inline-block;'>
                            Subir documentos
                        </a>
                    </p>
                    <p style='color:#666;font-size:12px;'>GMT · TecNM Campus Acapulco</p>
                </div>";

            await SendEmailAsync(correoEmpresa,
                "GMT — Tu cuenta empresarial está en revisión", htmlEmpresa);
        }

        /// <summary>Notifica a la empresa que fue aprobada.</summary>
        public async Task SendEmpresaAprobadaAsync(
            string correoEmpresa, string nombreEmpresa, string nombreContacto)
        {
            var html = $@"
                <div style='font-family:Arial,sans-serif;font-size:14px;color:#333;'>
                    <h2 style='color:#166534;'>¡Tu empresa fue verificada! ✓</h2>
                    <p>Hola <strong>{nombreContacto}</strong>,</p>
                    <p><strong>{nombreEmpresa}</strong> ha sido <strong>verificada</strong> por el departamento de Vinculación.</p>
                    <p>Ahora puedes publicar plazas de prácticas y recibir solicitudes de alumnos.</p>
                    <p>
                        <a href='https://gmtek.lol/Dashboard/Empresa'
                           style='background:#166534;color:#fff;padding:10px 22px;text-decoration:none;border-radius:6px;display:inline-block;'>
                            Ir a mi Dashboard
                        </a>
                    </p>
                    <p style='color:#666;font-size:12px;'>GMT · TecNM Campus Acapulco</p>
                </div>";

            await SendEmailAsync(correoEmpresa, "GMT — Tu empresa fue verificada ✓", html);
        }

        /// <summary>Notifica a la empresa que fue rechazada, con motivo.</summary>
        public async Task SendEmpresaRechazadaAsync(
            string correoEmpresa, string nombreEmpresa,
            string nombreContacto, string motivo)
        {
            var html = $@"
                <div style='font-family:Arial,sans-serif;font-size:14px;color:#333;'>
                    <h2 style='color:#991b1b;'>Revisión de tu empresa — acción requerida</h2>
                    <p>Hola <strong>{nombreContacto}</strong>,</p>
                    <p>La solicitud de verificación de <strong>{nombreEmpresa}</strong> no pudo ser aprobada:</p>
                    <div style='background:#fee2e2;border-left:4px solid #ef4444;padding:12px 16px;border-radius:4px;margin:16px 0;'>
                        <strong>Motivo:</strong> {motivo}
                    </div>
                    <p>Corrige la información y actualiza tus documentos desde tu portal.</p>
                    <p>
                        <a href='https://gmtek.lol/Dashboard/EmpresaDocumentos'
                           style='background:#002366;color:#fff;padding:10px 22px;text-decoration:none;border-radius:6px;display:inline-block;'>
                            Actualizar documentos
                        </a>
                    </p>
                    <p style='color:#666;font-size:12px;'>GMT · TecNM Campus Acapulco · {_correoVinculacion}</p>
                </div>";

            await SendEmailAsync(correoEmpresa, "GMT — Revisión de tu empresa: acción requerida", html);
        }

        /// <summary>
        /// Notifica a la empresa que un documento específico fue rechazado
        /// y necesita ser corregido y vuelto a subir.
        /// </summary>
        public async Task SendDocumentoRechazadoAsync(
            string correoEmpresa, string nombreEmpresa,
            string nombreContacto, string nombreArchivo, string motivo)
        {
            var html = $@"
                <div style='font-family:Arial,sans-serif;font-size:14px;color:#333;'>
                    <h2 style='color:#991b1b;'>Documento rechazado — acción requerida</h2>
                    <p>Hola <strong>{nombreContacto}</strong>,</p>
                    <p>Uno de los documentos de <strong>{nombreEmpresa}</strong> fue revisado y no puede ser aceptado:</p>
                    <table style='border-collapse:collapse;width:100%;margin:16px 0;'>
                        <tr style='background:#fee2e2;'>
                            <td style='padding:10px 14px;font-weight:bold;width:140px;'>Documento</td>
                            <td style='padding:10px 14px;font-family:monospace;font-size:12px;'>{nombreArchivo}</td>
                        </tr>
                        <tr>
                            <td style='padding:10px 14px;font-weight:bold;'>Motivo</td>
                            <td style='padding:10px 14px;color:#7f1d1d;'>{motivo}</td>
                        </tr>
                    </table>
                    <p>Por favor sube una versión corregida desde tu portal de documentos.</p>
                    <p>
                        <a href='https://gmtek.lol/Dashboard/EmpresaDocumentos'
                           style='background:#002366;color:#fff;padding:10px 22px;text-decoration:none;border-radius:6px;display:inline-block;'>
                            Subir documento corregido
                        </a>
                    </p>
                    <p style='color:#666;font-size:12px;'>GMT · TecNM Campus Acapulco · {_correoVinculacion}</p>
                </div>";

            await SendEmailAsync(correoEmpresa,
                $"GMT — Documento rechazado: {nombreArchivo}", html);
        }

        /// <summary>
        /// Notifica al alumno que la empresa le agendó una entrevista.
        /// </summary>
        public async Task SendEntrevistaAgendadaAsync(
            string correoAlumno,
            string nombreAlumno,
            string nombreEmpresa,
            string tituloPlaza,
            DateTimeOffset fechaHora,
            string modalidad,
            string ubicacionOLink,
            string notas)
        {
            var fechaStr = fechaHora.ToOffset(TimeSpan.FromHours(-6))
                               .ToString("dddd, d 'de' MMMM 'de' yyyy 'a las' HH:mm",
                                         new System.Globalization.CultureInfo("es-MX"));
            var modalidadStr = modalidad switch
            {
                "teams" => "Microsoft Teams",
                "zoom" => "Zoom",
                "meet" => "Google Meet",
                _ => "Presencial"
            };

            var html = $@"
                <div style='font-family:Arial,sans-serif;font-size:14px;color:#333;'>
                    <h2 style='color:#002366;'>🗓 Tienes una entrevista agendada</h2>
                    <p>Hola <strong>{nombreAlumno}</strong>,</p>
                    <p><strong>{nombreEmpresa}</strong> te ha invitado a una entrevista para la plaza
                       <strong>{tituloPlaza}</strong>.</p>

                    <table style='border-collapse:collapse;width:100%;margin:16px 0;'>
                        <tr style='background:#f0f4ff;'>
                            <td style='padding:10px 14px;font-weight:bold;width:160px;'>📅 Fecha y hora</td>
                            <td style='padding:10px 14px;font-weight:bold;color:#002366;'>{fechaStr}</td>
                        </tr>
                        <tr>
                            <td style='padding:10px 14px;font-weight:bold;'>📍 Modalidad</td>
                            <td style='padding:10px 14px;'>{modalidadStr}</td>
                        </tr>
                        {(!string.IsNullOrEmpty(ubicacionOLink) ? $@"
                        <tr style='background:#f0f4ff;'>
                            <td style='padding:10px 14px;font-weight:bold;'>🔗 Lugar / Link</td>
                            <td style='padding:10px 14px;'>{ubicacionOLink}</td>
                        </tr>" : "")}
                        {(!string.IsNullOrEmpty(notas) ? $@"
                        <tr>
                            <td style='padding:10px 14px;font-weight:bold;'>📝 Notas</td>
                            <td style='padding:10px 14px;'>{notas}</td>
                        </tr>" : "")}
                    </table>

                    <p>Por favor confirma tu asistencia desde tu portal GMT:</p>
                    <p>
                        <a href='https://gmtek.lol/Dashboard/Peticiones'
                           style='background:#002366;color:#fff;padding:10px 22px;
                                  text-decoration:none;border-radius:6px;display:inline-block;'>
                            Confirmar asistencia
                        </a>
                    </p>
                    <p style='color:#666;font-size:12px;'>GMT · TecNM Campus Acapulco</p>
                </div>";

            await SendEmailAsync(correoAlumno,
                $"GMT — Entrevista agendada: {nombreEmpresa} · {fechaStr}", html);
        }

        public string GetFromAddress() => _fromAddress;
    }
}
