using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SisReservaDeSalas.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriarTabelaSalas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Salas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Identificacao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Localizacao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Capacidade = table.Column<int>(type: "integer", nullable: false),
                    Recursos = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salas", x => x.Id);
                    table.CheckConstraint("CK_Salas_Capacidade", "\"Capacidade\" > 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Salas");
        }
    }
}
