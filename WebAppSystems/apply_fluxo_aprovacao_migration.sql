-- Script para aplicar manualmente a migração do fluxo de aprovação
-- Verifica e adiciona campos IsFinanceiro e IsAprovador na tabela Attorney

-- Verificar se as colunas já existem
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Attorney]') AND name = 'IsFinanceiro')
BEGIN
    ALTER TABLE [dbo].[Attorney] ADD [IsFinanceiro] bit NOT NULL DEFAULT 0;
    PRINT 'Coluna IsFinanceiro adicionada com sucesso';
END
ELSE
BEGIN
    PRINT 'Coluna IsFinanceiro já existe';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Attorney]') AND name = 'IsAprovador')
BEGIN
    ALTER TABLE [dbo].[Attorney] ADD [IsAprovador] bit NOT NULL DEFAULT 0;
    PRINT 'Coluna IsAprovador adicionada com sucesso';
END
ELSE
BEGIN
    PRINT 'Coluna IsAprovador já existe';
END

-- Verificar e adicionar campo EmAprovacao na tabela ProcessRecord
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcessRecord]') AND name = 'EmAprovacao')
BEGIN
    ALTER TABLE [dbo].[ProcessRecord] ADD [EmAprovacao] bit NOT NULL DEFAULT 0;
    PRINT 'Coluna EmAprovacao adicionada com sucesso';
END
ELSE
BEGIN
    PRINT 'Coluna EmAprovacao já existe';
END

-- Registrar a migração no histórico
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260509210000_AddFluxoAprovacaoFields')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260509210000_AddFluxoAprovacaoFields', '6.0.35');
    PRINT 'Migração registrada no histórico';
END
ELSE
BEGIN
    PRINT 'Migração já está registrada no histórico';
END

-- Verificar se as tabelas do fluxo de aprovação existem
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoteAprovacao')
BEGIN
    PRINT 'AVISO: Tabela LoteAprovacao não existe. Execute: dotnet ef database update';
END
ELSE
BEGIN
    PRINT 'Tabela LoteAprovacao existe';
END

PRINT 'Script concluído';
