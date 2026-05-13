
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

        [Required]
        [Column("login_id")]
        public int LoginId { get; set; }

        [Column("nombre_completo")]
        [StringLength(100)]
        public string? NombreCompleto { get; set; }

        [Column("numero_control")]
        [StringLength(20)]
        public string? NumeroControl { get; set; }

        [Column("carrera")]
        [StringLength(50)]
        public string? Carrera { get; set; }

        [Column("semestre")]
        [Range(1, 12)]
        public int? Semestre { get; set; }

        [Column("telefono")]
        [StringLength(15)]
        public string? Telefono { get; set; }

        [Column("foto_perfil_url")]
        public string? FotoPerfilUrl { get; set; }

        [Column("datos_completos")]
        public bool DatosCompletos { get; set; } = false;

        // Navegación
        [ForeignKey("LoginId")]
        public Login? Login { get; set; }

        public ICollection<DocumentoAlumno> Documentos { get; set; } = new List<DocumentoAlumno>();
        public ICollection<SolicitudPractica> Solicitudes { get; set; } = new List<SolicitudPractica>();
    }
}

//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace GMT.Models
//{
//    [Table("alumnos")]
//    public class Alumno
//    {
//        [Key]
//        [Column("id")]
//        public int Id { get; set; }

//        [Column("login_id")]
//        public int? LoginId { get; set; }

//        [Column("nombre_completo")]
//        public string? NombreCompleto { get; set; }

//        [Column("numero_control")]
//        public string? NumeroControl { get; set; }

//        [Column("carrera")]
//        public string? Carrera { get; set; }

//        [Column("semestre")]
//        public int Semestre { get; set; }

//        [Column("foto_perfil_url")]
//        public string? FotoPerfilUrl { get; set; }

//        // Relación virtual para navegar entre tablas en C#
//        [ForeignKey("LoginId")]
//        public virtual Login? Login { get; set; }
//    }
//}
