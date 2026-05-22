using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("entrevistas")]
    public class Entrevista
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("solicitud_id")]
        public int SolicitudId { get; set; }

        [Required]
        [Column("alumno_id")]
        public int AlumnoId { get; set; }

        [Required]
        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Required]
        [Column("fecha_hora")]
        public DateTimeOffset FechaHora { get; set; }

        // 'presencial' | 'teams' | 'zoom' | 'meet'
        [Required]
        [Column("modalidad")]
        [StringLength(20)]
        public string Modalidad { get; set; } = "presencial";

        [Column("ubicacion_o_link")]
        public string? UbicacionOLink { get; set; }

        [Column("notas")]
        public string? Notas { get; set; }

        // 'pendiente' | 'confirmada' | 'cancelada' | 'realizada'
        [Required]
        [Column("estado")]
        [StringLength(20)]
        public string Estado { get; set; } = "pendiente";

        [Column("confirmado_alumno")]
        public bool ConfirmadoAlumno { get; set; } = false;

        [Column("fecha_creacion")]
        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

        // Navegación
        [ForeignKey("SolicitudId")]
        public SolicitudPractica? Solicitud { get; set; }

        [ForeignKey("AlumnoId")]
        public Alumno? Alumno { get; set; }

        [ForeignKey("EmpresaId")]
        public Empresa? Empresa { get; set; }
    }
}
