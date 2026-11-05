using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class CreateTableClienteSolicitante : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClienteSolicitante",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteSolicitante", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClienteSolicitante_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClienteSolicitante_ClientId",
                table: "ClienteSolicitante",
                column: "ClientId");

            // Seed inicial: copia o solicitante padrao atual de cada cliente (nao vazio) para a nova tabela.
            migrationBuilder.Sql(@"
                INSERT INTO [ClienteSolicitante] ([ClientId], [Nome])
                SELECT [Id], LTRIM(RTRIM([Solicitante]))
                FROM [Client]
                WHERE [Solicitante] IS NOT NULL
                  AND LTRIM(RTRIM([Solicitante])) <> '';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClienteSolicitante");
        }
    }
}
