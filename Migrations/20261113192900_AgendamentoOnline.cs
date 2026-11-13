using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaS.Cilios.Migrations
{
    /// <inheritdoc />
    public partial class AgendamentoOnline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancelamentoHoras",
                table: "ConfiguracoesAgenda",
                type: "int",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<string>(
                name: "CodigoPublico",
                table: "Agendamentos",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentimentoEm",
                table: "Agendamentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_CodigoPublico",
                table: "Agendamentos",
                column: "CodigoPublico",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agendamentos_CodigoPublico",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "CancelamentoHoras",
                table: "ConfiguracoesAgenda");

            migrationBuilder.DropColumn(
                name: "CodigoPublico",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "ConsentimentoEm",
                table: "Agendamentos");
        }
    }
}
