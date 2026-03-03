using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AddDocumentAnalysisTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentAnalysis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByAttorneyId = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegalArea = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Complexity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstimatedHours = table.Column<int>(type: "int", nullable: true),
                    MainTopics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegalBasis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Parties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CauseValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Deadlines = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedAttorneys = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalysisStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AnalysisDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedToAttorneyId = table.Column<int>(type: "int", nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAnalysis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAnalysis_Attorney_AssignedToAttorneyId",
                        column: x => x.AssignedToAttorneyId,
                        principalTable: "Attorney",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentAnalysis_Attorney_UploadedByAttorneyId",
                        column: x => x.UploadedByAttorneyId,
                        principalTable: "Attorney",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentAnalysis_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAnalysis_AssignedToAttorneyId",
                table: "DocumentAnalysis",
                column: "AssignedToAttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAnalysis_ClientId",
                table: "DocumentAnalysis",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAnalysis_UploadedByAttorneyId",
                table: "DocumentAnalysis",
                column: "UploadedByAttorneyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentAnalysis");
        }
    }
}
