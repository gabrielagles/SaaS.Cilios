using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaS.Cilios.Migrations
{
    /// <inheritdoc />
    public partial class ContaConfiguracoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesConta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeExibicao = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    NomePainel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    FotoCaminho = table.Column<string>(type: "TEXT", nullable: true),
                    FotoVersao = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesConta", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesConta");
        }
    }
}
