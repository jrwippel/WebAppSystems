using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class CreateTableGestorArea : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GestorArea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GestorArea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GestorArea_Attorney_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Attorney",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GestorArea_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GestorArea_AttorneyId",
                table: "GestorArea",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_GestorArea_DepartmentId",
                table: "GestorArea",
                column: "DepartmentId");

            // Seed inicial: cada gestor atual (IsGestor=1) passa a gerenciar o próprio departamento,
            // preservando o comportamento de alertas que já existia.
            migrationBuilder.Sql(@"
                INSERT INTO [GestorArea] ([AttorneyId], [DepartmentId])
                SELECT [Id], [DepartmentId]
                FROM [Attorney]
                WHERE [IsGestor] = 1 AND [Inativo] = 0;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GestorArea");
        }
    }
}
