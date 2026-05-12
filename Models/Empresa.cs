using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    /// <summary>
    /// Modelo que representa una empresa registrada en la plataforma GMT.
    /// </summary>
    public class Empresa
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre completo de la empresa.
        /// </summary>
        [Required]
        public string? NombreEmpresa { get; set; }

        /// <summary>
        /// RFC de la empresa. Debe tener exactamente 12 caracteres.
        /// </summary>
        [Required]
        [StringLength(12, ErrorMessage = "El RFC debe contener exactamente 12 caracteres.")]
        public string? RFC { get; set; }

        /// <summary>
        /// Correo electrónico institucional de la empresa.
        /// </summary>
        [Required]
        public string? CorreoElectronico { get; set; }

        /// <summary>
        /// Contraseña cifrada de la cuenta de la empresa.
        /// </summary>
        [Required]
        public string? PasswordHash { get; set; }

        /// <summary>
        /// Indica si la cuenta de la empresa está verificada.
        /// </summary>
        [Required]
        public bool EstaVerificado { get; set; }

        /// <summary>
        /// Fecha de registro de la empresa.
        /// </summary>
        public DateTime FechaRegistro { get; set; }

        // Relación opcional con la cuenta de login (si existe)
        public int? LoginId { get; set; }
        public virtual Login? Login { get; set; }
    }
}
