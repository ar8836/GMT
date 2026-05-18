using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GMT.Models
{
    [Table("empresas")]
    public class Empresa
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("login_id")]
        public int? LoginId { get; set; }

        [Required]
        [Column("nombre_empresa")]
        public string NombreEmpresa { get; set; } = string.Empty;

        [Column("razon_social")]
        [StringLength(150)]
        public string? RazonSocial { get; set; }

        [Required]
        [Column("rfc")]
        [StringLength(13)]
        public string RFC { get; set; } = string.Empty;

        [Column("sector")]
        [StringLength(80)]
        public string? Sector { get; set; }

        [Column("giro")]
        [StringLength(120)]
        public string? Giro { get; set; }

        [Column("estado")]
        [StringLength(50)]
        public string? Estado { get; set; }

        [Column("ciudad")]
        [StringLength(80)]
        public string? Ciudad { get; set; }

        [Column("direccion")]
        public string? Direccion { get; set; }

        [Column("nombre_contacto")]
        [StringLength(100)]
        public string? NombreContacto { get; set; }

        [Column("puesto_contacto")]
        [StringLength(80)]
        public string? PuestoContacto { get; set; }

        [Column("telefono_contacto")]
        [StringLength(15)]
        public string? TelefonoContacto { get; set; }

        [Column("esta_verificado")]
        public bool EstaVerificado { get; set; } = false;

        [Column("fecha_registro")]
        public DateTimeOffset FechaRegistro { get; set; } = DateTimeOffset.UtcNow;

        [Column("datos_completos")]
        public bool DatosCompletos { get; set; } = false;

        // Navegación
        [ForeignKey("LoginId")]
        public Login? Login { get; set; }

        public ICollection<Convenio> Convenios { get; set; } = new List<Convenio>();
        public ICollection<SolicitudPractica> Solicitudes { get; set; } = new List<SolicitudPractica>();
        public ICollection<PlazaPractica> Plazas { get; set; } = new List<PlazaPractica>();
        public ICollection<DocumentoEmpresa> DocumentosEmpresa { get; set; } = new List<DocumentoEmpresa>();
    }
}
