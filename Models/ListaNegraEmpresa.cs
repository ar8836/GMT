using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("lista_negra_empresas")]
    public class ListaNegraEmpresa
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Required]
        [Column("motivo")]
        public string Motivo { get; set; } = string.Empty;

        [Column("agregado_por")]
        [StringLength(100)]
        public string? AgregadoPor { get; set; }

        [Column("fecha_ingreso")]
        public DateTimeOffset FechaIngreso { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// true = empresa bloqueada activamente.
        /// false = se levantó el bloqueo (historial conservado).
        /// </summary>
        [Column("activo")]
        public bool Activo { get; set; } = true;

        [ForeignKey("EmpresaId")]
        public Empresa? Empresa { get; set; }
    }
}
