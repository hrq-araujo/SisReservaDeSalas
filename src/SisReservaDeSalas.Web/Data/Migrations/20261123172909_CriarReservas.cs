using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SisReservaDeSalas.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriarReservas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalaId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Inicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Fim = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Finalidade = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.CheckConstraint("CK_Reservas_Periodo", "\"Fim\" > \"Inicio\"");
                    table.ForeignKey(
                        name: "FK_Reservas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Salas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_SalaId_Inicio_Fim",
                table: "Reservas",
                columns: new[] { "SalaId", "Inicio", "Fim" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioId",
                table: "Reservas",
                column: "UsuarioId");

            // Intervalo [início, fim): reservas consecutivas são permitidas.
            migrationBuilder.Sql("""
                ALTER TABLE "Reservas"
                ADD CONSTRAINT "EX_Reservas_SemSobreposicao"
                EXCLUDE USING gist (
                    "SalaId" WITH =,
                    tsrange("Inicio", "Fim", '[)') WITH &&
                ) WHERE ("Ativa");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");
        }
    }
}
