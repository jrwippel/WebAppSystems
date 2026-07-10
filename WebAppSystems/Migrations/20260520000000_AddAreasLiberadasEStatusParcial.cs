using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AddAreasLiberadasEStatusParcial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adicionar coluna AreasLiberadas na tabela LoteAprovacao
            // Armazena IDs de departamentos que já liberaram sua área (ex: "2,5,8")
            migrationBuilder.AddColumn<string>(
                name: "AreasLiberadas",
                table: "LoteAprovacao",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreasLiberadas",
                table: "LoteAprovacao");
        }
    }
}
