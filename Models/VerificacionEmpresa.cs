using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("verificaciones_empresa")]
    public class VerificacionEmpresa
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        // 'aprobada' | 'rechazada'
        [Required]
        [Column("decision")]
        [StringLength(20)]
        public string Decision { get; set; } = string.Empty;

        [Column("motivo")]
        public string? Motivo { get; set; }

        [Column("revisado_por")]
        [StringLength(100)]
        public string? RevisadoPor { get; set; }

        [Column("fecha_revision")]
        public DateTimeOffset FechaRevision { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey("EmpresaId")]
        public Empresa? Empresa { get; set; }
    }
}
