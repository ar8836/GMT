using System.ComponentModel.DataAnnotations;

namespace GMT.Models
{
    public class LoginViewModel
    {
        // ── Compartidos ──────────────────────────────────────────────────────
        [Display(Name = "Correo electrónico")]
        [EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "Contraseña")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Recordar mis credenciales")]
        public bool RecordarCredenciales { get; set; }

        public string? TipoRegistro { get; set; }  // 'Alumno' | 'Empresa'

        // ── Registro Alumno ───────────────────────────────────────────────────
        [Display(Name = "Nombre completo")]
        public string? NombreCompleto { get; set; }

        /// <summary>
        /// Número de control (matrícula). Mapeado a numero_control en DB.
        /// </summary>
        [Display(Name = "Número de control")]
        public string? Matricula { get; set; }

        [Display(Name = "Institución")]
        public string? Institucion { get; set; }

        [Display(Name = "Carrera")]
        public string? Carrera { get; set; }

        [Display(Name = "Semestre")]
        [Range(1, 12)]
        public int? Semestre { get; set; }

        [Display(Name = "Teléfono")]
        [Phone]
        public string? Telefono { get; set; }

        // ── Registro Empresa ──────────────────────────────────────────────────
        [Display(Name = "Nombre de la empresa")]
        public string? NombreEmpresa { get; set; }

        [Display(Name = "Razón social")]
        public string? RazonSocial { get; set; }

        [Display(Name = "RFC")]
        [StringLength(13, MinimumLength = 12)]
        public string? RFC { get; set; }

        [Display(Name = "Sector")]
        public string? Sector { get; set; }

        [Display(Name = "Giro / Actividad principal")]
        public string? Giro { get; set; }

        [Display(Name = "Ciudad")]
        public string? Ciudad { get; set; }

        [Display(Name = "Nombre del contacto")]
        public string? NombreContacto { get; set; }

        [Display(Name = "Puesto del contacto")]
        public string? PuestoContacto { get; set; }

        [Display(Name = "Teléfono de contacto")]
        [Phone]
        public string? TelefonoContacto { get; set; }
    }
}


//using System.ComponentModel.DataAnnotations;

//namespace GMT.Models
//{
//    public class LoginViewModel
//    {
//        // Login
//        [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
//        public string Email { get; set; } = string.Empty;

//        [DataType(DataType.Password)]
//        public string Password { get; set; } = string.Empty;

//        public bool RecordarCredenciales { get; set; }

//        // Alumno Registration
//        public string NombreCompleto { get; set; } = string.Empty;
//        public string Institucion { get; set; } = string.Empty;
//        public string Matricula { get; set; } = string.Empty;
//        public string Carrera { get; set; } = string.Empty;

//        // Empresa Registration
//        public string NombreEmpresa { get; set; } = string.Empty;
//        public string RFC { get; set; } = string.Empty;
//        public string Sector { get; set; } = string.Empty;

//        // Profile type (student/company) - not used in view currently but useful for backend
//        public string UserType { get; set; } = "student";
//    }
//}
