using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("DocumentosAlumno")]
    public class DocumentoAlumno
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("AlumnoId")]
        public int AlumnoId { get; set; }

        [Required]
        [Column("NombreArchivo")]
        [StringLength(260)]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        [Column("S3Key")]
        [StringLength(500)]
        public string S3Key { get; set; } = string.Empty;

        [Required]
        [Column("Url")]
        [StringLength(2048)]
        public string Url { get; set; } = string.Empty;

        [Required]
        [Column("TamañoBytes")]
        public long TamañoBytes { get; set; }

        // 'carta_presentacion' | 'seguro_imss' | 'reporte_parcial' | 'reporte_final' | 'otro'
        [Required]
        [Column("TipoDocumento")]
        [StringLength(50)]
        public string TipoDocumento { get; set; } = "otro";

        [Required]
        [Column("Estado")]
        [StringLength(20)]
        public string Estado { get; set; } = "pendiente";

        [Required]
        [Column("FechaSubida")]
        public DateTimeOffset FechaSubida { get; set; } = DateTimeOffset.UtcNow;

        // Navegación
        [ForeignKey("AlumnoId")]
        public Alumno? Alumno { get; set; }
    }
}


//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace GMT.Models
//{
//    [Table("DocumentosAlumno")]
//    public class DocumentoAlumno
//    {
//        [Key]
//        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
//        public int Id { get; set; }

//        [Required]
//        public int AlumnoId { get; set; }

//        [ForeignKey(nameof(AlumnoId))]
//        public Alumno? Alumno { get; set; }

//        [Required]
//        [MaxLength(260)]
//        public string NombreArchivo { get; set; } = string.Empty;

//        // Clave interna en el bucket S3 (para gestión)
//        [Required]
//        [MaxLength(500)]
//        public string S3Key { get; set; } = string.Empty;

//        // URL pre-firmada (puede expirar; regenerar al consultar)
//        [MaxLength(2048)]
//        public string Url { get; set; } = string.Empty;

//        public long TamañoBytes { get; set; }

//        // "Pendiente" | "Verificado" | "Rechazado"
//        [MaxLength(20)]
//        public string Estado { get; set; } = "Pendiente";

//        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
//    }
//}