using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class LimparImagemPadraoClientes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove a imagem genérica (default-image.jpg = 13536 bytes) de todos os clientes
            // Apenas afeta clientes com a imagem padrão do sistema, nunca logos reais
            migrationBuilder.Sql(@"
                UPDATE Client 
                SET ImageData = NULL, ImageMimeType = NULL 
                WHERE DATALENGTH(ImageData) = 13536
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Não é possível reverter (a imagem padrão precisaria ser reinserida manualmente)
        }
    }
}
