using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("login")] // Esto asegura que busque la tabla en minúsculas como en Postgres
    public class Login
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("correo_institucional")]
        public string CorreoInstitucional { get; set; }

        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Column("intentos_fallidos")]
        public int IntentosFallidos { get; set; }

        [Column("ultimo_acceso")]
        public DateTime? UltimoAcceso { get; set; }
    }
}
