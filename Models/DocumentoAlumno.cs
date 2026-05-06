using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("DocumentosAlumno")]
    public class DocumentoAlumno
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int AlumnoId { get; set; }

        [ForeignKey(nameof(AlumnoId))]
        public Alumno? Alumno { get; set; }

        [Required]
        [MaxLength(260)]
        public string NombreArchivo { get; set; } = string.Empty;

        // Clave interna en el bucket S3 (para gestión)
        [Required]
        [MaxLength(500)]
        public string S3Key { get; set; } = string.Empty;

        // URL pre-firmada (puede expirar; regenerar al consultar)
        [MaxLength(2048)]
        public string Url { get; set; } = string.Empty;

        public long TamañoBytes { get; set; }

        // "Pendiente" | "Verificado" | "Rechazado"
        [MaxLength(20)]
        public string Estado { get; set; } = "Pendiente";

        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
    }
}