using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AlterTablePercentualArea : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PercentualArea_ClientId",
                table: "PercentualArea",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_PercentualArea_DepartmentId",
                table: "PercentualArea",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PercentualArea_Client_ClientId",
                table: "PercentualArea",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PercentualArea_Department_DepartmentId",
                table: "PercentualArea",
                column: "DepartmentId",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PercentualArea_Client_ClientId",
                table: "PercentualArea");

            migrationBuilder.DropForeignKey(
                name: "FK_PercentualArea_Department_DepartmentId",
                table: "PercentualArea");

            migrationBuilder.DropIndex(
                name: "IX_PercentualArea_ClientId",
                table: "PercentualArea");

            migrationBuilder.DropIndex(
                name: "IX_PercentualArea_DepartmentId",
                table: "PercentualArea");
        }
    }
}
