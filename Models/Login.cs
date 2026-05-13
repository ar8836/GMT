using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("login")]
    public class Login
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("correo_institucional")]
        [StringLength(100)]
        public string CorreoInstitucional { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        // 'alumno' | 'empresa' | 'admin'
        [Required]
        [Column("rol")]
        [StringLength(20)]
        public string Rol { get; set; } = "alumno";

        [Column("intentos_fallidos")]
        public int IntentosFallidos { get; set; } = 0;

        [Column("ultimo_acceso")]
        public DateTime? UltimoAcceso { get; set; }

        [Column("es_verificado")]
        public bool EsVerificado { get; set; } = false;

        [Column("token_verificacion")]
        [StringLength(100)]
        public string? TokenVerificacion { get; set; }

        [Column("fecha_expiracion_token")]
        public DateTime? FechaExpiracionToken { get; set; }

        // Navegación
        public Alumno? Alumno { get; set; }
        public Empresa? Empresa { get; set; }
    }
}


//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace GMT.Models
//{
//    [Table("login")]
//    public class Login
//    {
//        [Key]
//        [Column("id")]
//        public int Id { get; set; }

//        [Column("correo_institucional")]
//        public required string CorreoInstitucional { get; set; }

//        [Column("password_hash")]
//        public required string PasswordHash { get; set; }

//        [Column("intentos_fallidos")]
//        public int IntentosFallidos { get; set; } = 0;

//        [Column("ultimo_acceso")]
//        public DateTime? UltimoAcceso { get; set; }

//        [Column("es_verificado")]
//        public bool EstaVerificado { get; set; } = false;
//    }
//}
