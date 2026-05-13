using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("empresas")]
    public class Empresa
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("login_id")]
        public int? LoginId { get; set; }

        [Required]
        [Column("nombre_empresa")]
        public string NombreEmpresa { get; set; } = string.Empty;

        [Column("razon_social")]
        [StringLength(150)]
        public string? RazonSocial { get; set; }

        [Required]
        [Column("rfc")]
        [StringLength(13)]
        public string RFC { get; set; } = string.Empty;

        // Ej: 'Tecnología', 'Manufactura', 'Salud', etc.
        [Column("sector")]
        [StringLength(80)]
        public string? Sector { get; set; }

        [Column("giro")]
        [StringLength(120)]
        public string? Giro { get; set; }

        [Column("estado")]
        [StringLength(50)]
        public string? Estado { get; set; }

        [Column("ciudad")]
        [StringLength(80)]
        public string? Ciudad { get; set; }

        [Column("direccion")]
        public string? Direccion { get; set; }

        [Column("nombre_contacto")]
        [StringLength(100)]
        public string? NombreContacto { get; set; }

        [Column("puesto_contacto")]
        [StringLength(80)]
        public string? PuestoContacto { get; set; }

        [Column("telefono_contacto")]
        [StringLength(15)]
        public string? TelefonoContacto { get; set; }

        [Column("esta_verificado")]
        public bool EstaVerificado { get; set; } = false;

        [Column("fecha_registro")]
        public DateTimeOffset FechaRegistro { get; set; } = DateTimeOffset.UtcNow;

        [Column("datos_completos")]
        public bool DatosCompletos { get; set; } = false;

        // Navegación
        [ForeignKey("LoginId")]
        public Login? Login { get; set; }

        public ICollection<Convenio> Convenios { get; set; } = new List<Convenio>();
        public ICollection<SolicitudPractica> Solicitudes { get; set; } = new List<SolicitudPractica>();
    }
}


//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace GMT.Models
//{
//    /// <summary>
//    /// Modelo que representa una empresa registrada en la plataforma GMT.
//    /// </summary>
//    public class Empresa
//    {
//        [Key]
//        public int Id { get; set; }

//        /// <summary>
//        /// Nombre completo de la empresa.
//        /// </summary>
//        [Required]
//        public string? NombreEmpresa { get; set; }

//        /// <summary>
//        /// RFC de la empresa. Debe tener exactamente 12 caracteres.
//        /// </summary>
//        [Required]
//        [StringLength(12, ErrorMessage = "El RFC debe contener exactamente 12 caracteres.")]
//        public string? RFC { get; set; }

//        /// <summary>
//        /// Correo electrónico institucional de la empresa.
//        /// </summary>
//        [Required]
//        public string? CorreoElectronico { get; set; }

//        /// <summary>
//        /// Contraseña cifrada de la cuenta de la empresa.
//        /// </summary>
//        [Required]
//        public string? PasswordHash { get; set; }

//        /// <summary>
//        /// Indica si la cuenta de la empresa está verificada.
//        /// </summary>
//        [Required]
//        public bool EstaVerificado { get; set; }

//        /// <summary>
//        /// Fecha de registro de la empresa.
//        /// </summary>
//        public DateTime FechaRegistro { get; set; }

//        // Relación opcional con la cuenta de login (si existe)
//        public int? LoginId { get; set; }
//        public virtual Login? Login { get; set; }
//    }
//}
