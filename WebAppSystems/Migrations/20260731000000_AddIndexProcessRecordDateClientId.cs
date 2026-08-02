using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AddIndexProcessRecordDateClientId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Índice composto para acelerar queries por Date + ClientId (usado em rentabilidade)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProcessRecord_Date_ClientId' AND object_id = OBJECT_ID('ProcessRecord'))
                BEGIN
                    CREATE NONCLUSTERED INDEX [IX_ProcessRecord_Date_ClientId] 
                    ON [ProcessRecord] ([Date], [ClientId]) 
                    INCLUDE ([HoraInicial], [HoraFinal], [AttorneyId], [DepartmentId], [RecordType])
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProcessRecord_Date_ClientId' AND object_id = OBJECT_ID('ProcessRecord'))
                BEGIN
                    DROP INDEX [IX_ProcessRecord_Date_ClientId] ON [ProcessRecord]
                END
            ");
        }
    }
}
