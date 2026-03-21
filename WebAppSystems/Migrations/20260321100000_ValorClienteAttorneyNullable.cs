using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    /// <inheritdoc />
    public partial class ValorClienteAttorneyNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove FK e índice existentes
            migrationBuilder.DropForeignKey(
                name: "FK_ValorCliente_Attorney_AttorneyId",
                table: "ValorCliente");

            // Torna AttorneyId nullable
            migrationBuilder.AlterColumn<int>(
                name: "AttorneyId",
                table: "ValorCliente",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Recria FK como nullable
            migrationBuilder.AddForeignKey(
                name: "FK_ValorCliente_Attorney_AttorneyId",
                table: "ValorCliente",
                column: "AttorneyId",
                principalTable: "Attorney",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValorCliente_Attorney_AttorneyId",
                table: "ValorCliente");

            migrationBuilder.AlterColumn<int>(
                name: "AttorneyId",
                table: "ValorCliente",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ValorCliente_Attorney_AttorneyId",
                table: "ValorCliente",
                column: "AttorneyId",
                principalTable: "Attorney",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
