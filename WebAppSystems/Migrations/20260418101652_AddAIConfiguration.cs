using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AddAIConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValorCliente_Attorney_AttorneyId",
                table: "ValorCliente");

            migrationBuilder.CreateTable(
                name: "AIConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConfiguration", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ValorCliente_Attorney_AttorneyId",
                table: "ValorCliente",
                column: "AttorneyId",
                principalTable: "Attorney",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValorCliente_Attorney_AttorneyId",
                table: "ValorCliente");

            migrationBuilder.DropTable(
                name: "AIConfiguration");

            migrationBuilder.AddForeignKey(
                name: "FK_ValorCliente_Attorney_AttorneyId",
                table: "ValorCliente",
                column: "AttorneyId",
                principalTable: "Attorney",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
