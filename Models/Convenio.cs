using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("convenios")]
    public class Convenio
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Column("numero_convenio")]
        [StringLength(50)]
        public string? NumeroConvenio { get; set; }

        // 'en_tramite' | 'vigente' | 'vencido'
        [Required]
        [Column("estado")]
        [StringLength(20)]
        public string Estado { get; set; } = "en_tramite";

        [Column("fecha_inicio")]
        public DateOnly? FechaInicio { get; set; }

        [Column("fecha_fin")]
        public DateOnly? FechaFin { get; set; }

        [Column("archivo_url")]
        public string? ArchivoUrl { get; set; }

        // Navegación
        [ForeignKey("EmpresaId")]
        public Empresa? Empresa { get; set; }

        public ICollection<SolicitudPractica> Solicitudes { get; set; } = new List<SolicitudPractica>();
    }
}
