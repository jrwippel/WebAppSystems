using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class LimparImagemPadraoClientesEmLotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Limpa imagens padrão em lotes de 10 para evitar timeout
            // A imagem default-image.jpg tem exatamente 13536 bytes
            migrationBuilder.Sql(@"
                DECLARE @BatchSize INT = 10;
                DECLARE @RowsAffected INT = 1;

                WHILE @RowsAffected > 0
                BEGIN
                    UPDATE TOP (@BatchSize) Client 
                    SET ImageData = NULL, ImageMimeType = NULL 
                    WHERE DATALENGTH(ImageData) = 13536;

                    SET @RowsAffected = @@ROWCOUNT;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Não reversível
        }
    }
}
