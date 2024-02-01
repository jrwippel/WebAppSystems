using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class ClienteIdProcessRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "ProcessRecord",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessRecord_ClientId",
                table: "ProcessRecord",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessRecord_Client_ClientId",
                table: "ProcessRecord",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessRecord_Client_ClientId",
                table: "ProcessRecord");

            migrationBuilder.DropIndex(
                name: "IX_ProcessRecord_ClientId",
                table: "ProcessRecord");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "ProcessRecord");
        }
    }
}
