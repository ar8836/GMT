using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("plazas_practicas")]
    public class PlazaPractica
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        // ── Descripción ──────────────────────────────────────────────────────
        [Required]
        [Column("titulo")]
        [StringLength(150)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [Column("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("area")]
        [StringLength(100)]
        public string? Area { get; set; }

        // 'presencial' | 'remoto' | 'hibrido'
        [Required]
        [Column("modalidad")]
        [StringLength(20)]
        public string Modalidad { get; set; } = "presencial";

        // ── Requisitos ───────────────────────────────────────────────────────
        /// <summary>JSON array de claves de carrera, e.g. ["ISC","IIA"]. Null = todas.</summary>
        [Column("carreras_requeridas")]
        public string? CarrerasRequeridas { get; set; }

        [Column("semestre_minimo")]
        [Range(1, 10)]
        public int? SemestreMinimo { get; set; }

        // ── Cupos y fechas ───────────────────────────────────────────────────
        [Column("cupos_disponibles")]
        public int CuposDisponibles { get; set; } = 1;

        [Column("cupos_ocupados")]
        public int CuposOcupados { get; set; } = 0;

        [Column("fecha_inicio")]
        public DateOnly? FechaInicio { get; set; }

        [Column("fecha_fin")]
        public DateOnly? FechaFin { get; set; }

        // ── Estado ───────────────────────────────────────────────────────────
        // 'borrador' | 'activa' | 'pausada' | 'cerrada'
        [Required]
        [Column("estado")]
        [StringLength(20)]
        public string Estado { get; set; } = "borrador";

        // ── Auditoría ────────────────────────────────────────────────────────
        [Column("fecha_publicacion")]
        public DateTimeOffset? FechaPublicacion { get; set; }

        [Column("fecha_creacion")]
        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

        [Column("fecha_actualizacion")]
        public DateTimeOffset FechaActualizacion { get; set; } = DateTimeOffset.UtcNow;

        // ── Navegación ───────────────────────────────────────────────────────
        [ForeignKey("EmpresaId")]
        public Empresa? Empresa { get; set; }

        public ICollection<SolicitudPractica> Solicitudes { get; set; } = new List<SolicitudPractica>();

        // ── Computed helpers (no mapeados) ───────────────────────────────────
        [NotMapped]
        public int CuposRestantes => Math.Max(0, CuposDisponibles - CuposOcupados);

        [NotMapped]
        public bool TieneEspacio => CuposRestantes > 0;

        [NotMapped]
        public bool EsActiva => Estado == "activa";
    }
}

