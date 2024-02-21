using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class RemovePrecoClienteTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
            name: "PrecoCliente");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
