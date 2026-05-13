using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GMT.Migrations
{
    /// <inheritdoc />
    public partial class EsquemaV2_Limpio : Migration
    {
        // La DB ya fue creada manualmente con schema_v2.sql directamente en RDS.
        // Up() y Down() están vacíos a propósito para que EF solo registre
        // el historial sin intentar re-crear nada.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) { }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
