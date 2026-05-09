using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    /// <inheritdoc />
    public partial class AddFaturamentoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFaturado",
                table: "ProcessRecord",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataFaturamento",
                table: "ProcessRecord",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FaturadoPorId",
                table: "ProcessRecord",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessRecord_FaturadoPorId",
                table: "ProcessRecord",
                column: "FaturadoPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessRecord_Attorney_FaturadoPorId",
                table: "ProcessRecord",
                column: "FaturadoPorId",
                principalTable: "Attorney",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessRecord_Attorney_FaturadoPorId",
                table: "ProcessRecord");

            migrationBuilder.DropIndex(
                name: "IX_ProcessRecord_FaturadoPorId",
                table: "ProcessRecord");

            migrationBuilder.DropColumn(
                name: "IsFaturado",
                table: "ProcessRecord");

            migrationBuilder.DropColumn(
                name: "DataFaturamento",
                table: "ProcessRecord");

            migrationBuilder.DropColumn(
                name: "FaturadoPorId",
                table: "ProcessRecord");
        }
    }
}
