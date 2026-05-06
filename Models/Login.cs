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

        [Column("correo_institucional")]
        public required string CorreoInstitucional { get; set; }

        [Column("password_hash")]
        public required string PasswordHash { get; set; }

        [Column("intentos_fallidos")]
        public int IntentosFallidos { get; set; } = 0;

        [Column("ultimo_acceso")]
        public DateTime? UltimoAcceso { get; set; }

        [Column("es_verificado")]
        public bool EstaVerificado { get; set; } = false;
    }
}
