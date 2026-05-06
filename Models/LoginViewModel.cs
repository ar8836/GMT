using System.ComponentModel.DataAnnotations;

namespace GMT.Models
{
    public class LoginViewModel
    {
        // Login
        [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RecordarCredenciales { get; set; }

        // Alumno Registration
        public string NombreCompleto { get; set; } = string.Empty;
        public string Institucion { get; set; } = string.Empty;
        public string Matricula { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;

        // Empresa Registration
        public string NombreEmpresa { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;

        // Profile type (student/company) - not used in view currently but useful for backend
        public string UserType { get; set; } = "student";
    }
}
