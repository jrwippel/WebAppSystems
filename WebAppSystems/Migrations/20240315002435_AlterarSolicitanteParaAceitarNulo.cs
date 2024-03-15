using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AlterarSolicitanteParaAceitarNulo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
            name: "Solicitante",
            table: "Client",
            type: "nvarchar(max)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldNullable: false,
            oldDefaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
