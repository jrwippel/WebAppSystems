using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AlterTableProcessRecordSolicitanteDepartment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ProcessRecord",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "ProcessRecord",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Solicitante",
                table: "ProcessRecord",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Client",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessRecord_DepartmentId",
                table: "ProcessRecord",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessRecord_Department_DepartmentId",
                table: "ProcessRecord",
                column: "DepartmentId",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessRecord_Department_DepartmentId",
                table: "ProcessRecord");

            migrationBuilder.DropIndex(
                name: "IX_ProcessRecord_DepartmentId",
                table: "ProcessRecord");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ProcessRecord");

            migrationBuilder.DropColumn(
                name: "Solicitante",
                table: "ProcessRecord");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ProcessRecord",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Client",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);
        }
    }
}
