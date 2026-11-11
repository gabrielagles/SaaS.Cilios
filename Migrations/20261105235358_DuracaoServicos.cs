using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaS.Cilios.Migrations
{
    /// <inheritdoc />
    public partial class DuracaoServicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuracaoMinutos",
                table: "Servicos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DuracaoMinutos",
                table: "Agendamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuracaoMinutos",
                table: "Servicos");

            migrationBuilder.DropColumn(
                name: "DuracaoMinutos",
                table: "Agendamentos");
        }
    }
}
