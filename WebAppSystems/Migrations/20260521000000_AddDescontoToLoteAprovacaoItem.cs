using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AddDescontoToLoteAprovacaoItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PercentualDesconto",
                table: "LoteAprovacaoItem",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificativaDesconto",
                table: "LoteAprovacaoItem",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PercentualDesconto", table: "LoteAprovacaoItem");
            migrationBuilder.DropColumn(name: "JustificativaDesconto", table: "LoteAprovacaoItem");
        }
    }
}
