using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AddPadraoToClienteSolicitante : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Padrao",
                table: "ClienteSolicitante",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Marca como padrão o solicitante que veio do cadastro antigo do cliente (Client.Solicitante),
            // para que a sugestão nos lançamentos continue a mesma de antes. No máximo um por cliente.
            migrationBuilder.Sql(@"
                WITH alvo AS (
                    SELECT cs.Id,
                           ROW_NUMBER() OVER (PARTITION BY cs.ClientId ORDER BY cs.Id) AS rn
                    FROM [ClienteSolicitante] cs
                    INNER JOIN [Client] c ON c.Id = cs.ClientId
                    WHERE c.Solicitante IS NOT NULL
                      AND LTRIM(RTRIM(c.Solicitante)) <> ''
                      AND cs.Nome = LTRIM(RTRIM(c.Solicitante))
                )
                UPDATE cs SET cs.Padrao = 1
                FROM [ClienteSolicitante] cs
                INNER JOIN alvo ON alvo.Id = cs.Id
                WHERE alvo.rn = 1;
            ");

            // Clientes que ainda ficaram sem padrão mas têm um único solicitante: esse vira o padrão.
            // (Com vários solicitantes não há como saber qual é o padrão; fica para definir na tela.)
            migrationBuilder.Sql(@"
                UPDATE cs SET cs.Padrao = 1
                FROM [ClienteSolicitante] cs
                WHERE cs.ClientId IN (
                    SELECT ClientId
                    FROM [ClienteSolicitante]
                    GROUP BY ClientId
                    HAVING COUNT(*) = 1 AND SUM(CASE WHEN Padrao = 1 THEN 1 ELSE 0 END) = 0
                );
            ");

            // Campo legado do cliente acompanha o padrão definido acima
            migrationBuilder.Sql(@"
                UPDATE c SET c.Solicitante = cs.Nome
                FROM [Client] c
                INNER JOIN [ClienteSolicitante] cs ON cs.ClientId = c.Id AND cs.Padrao = 1
                WHERE c.Solicitante IS NULL OR LTRIM(RTRIM(c.Solicitante)) = '';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Padrao",
                table: "ClienteSolicitante");
        }
    }
}
