using Amazon.SimpleEmail; // AWS SES
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
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _sesClient = null; // Lazy initialization
        }
        private IAmazonSimpleEmailService GetSesClient()
        {
            if (_sesClient == null)
            {
                var accessKey = _configuration["AWS:AccessKey"];
                var secretKey = _configuration["AWS:SecretKey"];
                var region = _configuration["AWS:Region"] ?? "";

                Amazon.Runtime.AWSCredentials credentials;
                if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
                {
                    credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
                }
                else
                {
                    credentials = Amazon.Runtime.FallbackCredentialsFactory.GetCredentials();
                }

                var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
                _sesClient = new Amazon.SimpleEmail.AmazonSimpleEmailServiceClient(credentials, regionEndpoint);
            }
            return _sesClient;
        }

        /// <summary>
        /// Envía un correo de bienvenida personalizado.
        /// </summary>
        public async Task SendWelcomeEmailAsync(string toEmail, string studentName)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) throw new ArgumentException("No email provided", nameof(toEmail));

            var subject = new Content("Bienvenido a GMT");
            var htmlBody = new Content($"<h1>Hola {studentName}</h1><p>Bienvenido a <a href='https://gmtek.lol/'>GMT</a>. ¡Nos alegra contar contigo!</p>");
            var body = new Body { Html = htmlBody };

            var sendRequest = new SendEmailRequest
            {
                Source = _fromAddress,
                Destination = new Destination { ToAddresses = { toEmail } },
                Message = new Message
                {
                    Subject = subject,
                    Body = body
                }
            };

            await GetSesClient().SendEmailAsync(sendRequest);
        }

        /// <summary>
        /// Envía un correo genérico con asunto y cuerpo HTML personalizado.
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) throw new ArgumentException("No email provided", nameof(toEmail));
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required", nameof(subject));
            if (string.IsNullOrWhiteSpace(htmlBody)) throw new ArgumentException("Body is required", nameof(htmlBody));

            var subjectContent = new Content(subject);
            var bodyContent = new Body { Html = new Content(htmlBody) };

            var sendRequest = new SendEmailRequest
            {
                Source = _fromAddress,
                Destination = new Destination { ToAddresses = { toEmail } },
                Message = new Message
                {
                    Subject = subjectContent,
                    Body = bodyContent
                }
            };

            await GetSesClient().SendEmailAsync(sendRequest);
        }

        /// <summary>
        /// Envía un correo de verificación con un enlace único.
        /// </summary>
        public async Task SendVerificationEmailAsync(string toEmail, string verificationLink)
        {
            string subject = "Verificación de cuenta - GMT";
            string htmlBody = $@"
                <div style='font-family: Arial, sans-serif; font-size: 14px; color: #333;'>
                    <h2 style='color: #2c3e50;'>Verificación de cuenta GMT</h2>
                    <p>Gracias por registrarte en GMT.</p>
                    <p>Para completar la activación de tu cuenta, haz clic en el siguiente enlace:</p>
                    <p><a href='{verificationLink}' style='background-color: #3498db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Activar cuenta</a></p>
                    <p>O copia y pega el siguiente enlace en tu navegador:</p>
                    <p>{verificationLink}</p>
                    <p><small>Este enlace expirará en 24 horas.</small></p>
                    <hr>
                    <p><small>Si no te registraste en GMT, ignora este correo.</small></p>
                </div>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        // Método auxiliar para obtener la dirección de origen
        public string GetFromAddress() => _fromAddress;
    }
}
