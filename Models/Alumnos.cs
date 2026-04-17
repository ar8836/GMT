using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("alumnos")]
    public class Alumno
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("login_id")]
        public int LoginId { get; set; }

        [Column("nombre_completo")]
        public string NombreCompleto { get; set; }

        [Column("numero_control")]
        public string NumeroControl { get; set; }

        [Column("carrera")]
        public string Carrera { get; set; }

        [Column("semestre")]
        public int Semestre { get; set; }

        // Relación virtual para navegar entre tablas en C#
        [ForeignKey("LoginId")]
        public virtual Login Login { get; set; }
    }
}
