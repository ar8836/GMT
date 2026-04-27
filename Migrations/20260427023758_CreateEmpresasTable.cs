using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GMT.Migrations
{
    /// <inheritdoc />
    public partial class CreateEmpresasTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreEmpresa = table.Column<string>(type: "text", nullable: false),
                    RFC = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CorreoElectronico = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    EstaVerificado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LoginId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_empresas_login_LoginId",
                        column: x => x.LoginId,
                        principalTable: "login",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_empresas_LoginId",
                table: "empresas",
                column: "LoginId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "empresas");
        }
    }
}
