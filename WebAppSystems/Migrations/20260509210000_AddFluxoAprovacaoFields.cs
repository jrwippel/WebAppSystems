using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSystems.Migrations
{
    public partial class AddFluxoAprovacaoFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adicionar campos IsFinanceiro e IsAprovador na tabela Attorney
            migrationBuilder.AddColumn<bool>(
                name: "IsFinanceiro",
                table: "Attorney",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAprovador",
                table: "Attorney",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Adicionar campo EmAprovacao na tabela ProcessRecord
            migrationBuilder.AddColumn<bool>(
                name: "EmAprovacao",
                table: "ProcessRecord",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Criar tabela LoteAprovacao
            migrationBuilder.CreateTable(
                name: "LoteAprovacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CriadoPorId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    PeriodoInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodoFim = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalHoras = table.Column<double>(type: "float", nullable: false),
                    ValorEstimado = table.Column<double>(type: "float", nullable: false),
                    DataAprovacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AprovadoPorId = table.Column<int>(type: "int", nullable: true),
                    ComentarioAprovador = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataFaturamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FaturadoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoteAprovacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoteAprovacao_Attorney_AprovadoPorId",
                        column: x => x.AprovadoPorId,
                        principalTable: "Attorney",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LoteAprovacao_Attorney_CriadoPorId",
                        column: x => x.CriadoPorId,
                        principalTable: "Attorney",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoteAprovacao_Attorney_FaturadoPorId",
                        column: x => x.FaturadoPorId,
                        principalTable: "Attorney",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LoteAprovacao_Client_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Criar tabela LoteAprovacaoItem
            migrationBuilder.CreateTable(
                name: "LoteAprovacaoItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoteAprovacaoId = table.Column<int>(type: "int", nullable: false),
                    ProcessRecordId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Abonado = table.Column<bool>(type: "bit", nullable: false),
                    DataRevisao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ObservacaoRevisao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FoiEditado = table.Column<bool>(type: "bit", nullable: false),
                    DescricaoOriginal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HoraInicialOriginal = table.Column<TimeSpan>(type: "time", nullable: true),
                    HoraFinalOriginal = table.Column<TimeSpan>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoteAprovacaoItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoteAprovacaoItem_LoteAprovacao_LoteAprovacaoId",
                        column: x => x.LoteAprovacaoId,
                        principalTable: "LoteAprovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoteAprovacaoItem_ProcessRecord_ProcessRecordId",
                        column: x => x.ProcessRecordId,
                        principalTable: "ProcessRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Criar tabela HistoricoAprovacao
            migrationBuilder.CreateTable(
                name: "HistoricoAprovacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoteAprovacaoId = table.Column<int>(type: "int", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    TipoAcao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detalhes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProcessRecordId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoAprovacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoAprovacao_Attorney_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Attorney",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoricoAprovacao_LoteAprovacao_LoteAprovacaoId",
                        column: x => x.LoteAprovacaoId,
                        principalTable: "LoteAprovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistoricoAprovacao_ProcessRecord_ProcessRecordId",
                        column: x => x.ProcessRecordId,
                        principalTable: "ProcessRecord",
                        principalColumn: "Id");
                });

            // Criar tabela NotificacaoAprovacao
            migrationBuilder.CreateTable(
                name: "NotificacaoAprovacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    LoteAprovacaoId = table.Column<int>(type: "int", nullable: false),
                    TipoNotificacao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Lida = table.Column<bool>(type: "bit", nullable: false),
                    DataLeitura = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacaoAprovacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacaoAprovacao_Attorney_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Attorney",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificacaoAprovacao_LoteAprovacao_LoteAprovacaoId",
                        column: x => x.LoteAprovacaoId,
                        principalTable: "LoteAprovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Criar índices
            migrationBuilder.CreateIndex(
                name: "IX_LoteAprovacao_AprovadoPorId",
                table: "LoteAprovacao",
                column: "AprovadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_LoteAprovacao_ClienteId",
                table: "LoteAprovacao",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_LoteAprovacao_CriadoPorId",
                table: "LoteAprovacao",
                column: "CriadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_LoteAprovacao_FaturadoPorId",
                table: "LoteAprovacao",
                column: "FaturadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_LoteAprovacaoItem_LoteAprovacaoId",
                table: "LoteAprovacaoItem",
                column: "LoteAprovacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LoteAprovacaoItem_ProcessRecordId",
                table: "LoteAprovacaoItem",
                column: "ProcessRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAprovacao_LoteAprovacaoId",
                table: "HistoricoAprovacao",
                column: "LoteAprovacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAprovacao_ProcessRecordId",
                table: "HistoricoAprovacao",
                column: "ProcessRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAprovacao_UsuarioId",
                table: "HistoricoAprovacao",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacaoAprovacao_LoteAprovacaoId",
                table: "NotificacaoAprovacao",
                column: "LoteAprovacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacaoAprovacao_UsuarioId",
                table: "NotificacaoAprovacao",
                column: "UsuarioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remover tabelas
            migrationBuilder.DropTable(name: "HistoricoAprovacao");
            migrationBuilder.DropTable(name: "LoteAprovacaoItem");
            migrationBuilder.DropTable(name: "NotificacaoAprovacao");
            migrationBuilder.DropTable(name: "LoteAprovacao");

            // Remover colunas
            migrationBuilder.DropColumn(name: "IsFinanceiro", table: "Attorney");
            migrationBuilder.DropColumn(name: "IsAprovador", table: "Attorney");
            migrationBuilder.DropColumn(name: "EmAprovacao", table: "ProcessRecord");
        }
    }
}
