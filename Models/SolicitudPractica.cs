using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("solicitudes_practicas")]
    public class SolicitudPractica
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("alumno_id")]
        public int AlumnoId { get; set; }

        [Required]
        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Column("convenio_id")]
        public int? ConvenioId { get; set; }

        // 'solicitado' | 'aceptado' | 'en_curso' | 'concluido' | 'rechazado'
        [Required]
        [Column("estado")]
        [StringLength(30)]
        public string Estado { get; set; } = "solicitado";

        [Column("fecha_solicitud")]
        public DateTimeOffset FechaSolicitud { get; set; } = DateTimeOffset.UtcNow;

        [Column("fecha_inicio")]
        public DateOnly? FechaInicio { get; set; }

        [Column("fecha_fin")]
        public DateOnly? FechaFin { get; set; }

        [Column("proyecto")]
        [StringLength(200)]
        public string? Proyecto { get; set; }

        [Column("area")]
        [StringLength(100)]
        public string? Area { get; set; }

        // Navegación
        [ForeignKey("AlumnoId")]
        public Alumno? Alumno { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa? Empresa { get; set; }

        [ForeignKey("ConvenioId")]
        public Convenio? Convenio { get; set; }
    }
}
