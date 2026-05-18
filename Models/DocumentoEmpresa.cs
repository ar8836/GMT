using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("documentos_empresa")]
    public class DocumentoEmpresa
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("empresa_id")]
        public int EmpresaId { get; set; }

        [Required]
        [Column("nombre_archivo")]
        [StringLength(260)]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        [Column("s3_key")]
        [StringLength(500)]
        public string S3Key { get; set; } = string.Empty;

        [Required]
        [Column("url")]
        [StringLength(2048)]
        public string Url { get; set; } = string.Empty;

        [Required]
        [Column("tamano_bytes")]
        public long TamanoBytes { get; set; }

        // 'convenio' | 'acta_constitutiva' | 'comprobante_domicilio' | 'otro'
        [Required]
        [Column("tipo_documento")]
        [StringLength(50)]
        public string TipoDocumento { get; set; } = "otro";

        // 'pendiente' | 'aprobado' | 'rechazado'
        [Required]
        [Column("estado")]
        [StringLength(20)]
        public string Estado { get; set; } = "pendiente";

        [Required]
        [Column("fecha_subida")]
        public DateTimeOffset FechaSubida { get; set; } = DateTimeOffset.UtcNow;

        // Navegación
        [ForeignKey("EmpresaId")]
        public Empresa? Empresa { get; set; }
    }
}
