-- Adicionar campos IsFinanceiro e IsAprovador na tabela Attorney
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Attorney]') AND name = 'IsFinanceiro')
BEGIN
    ALTER TABLE [Attorney] ADD [IsFinanceiro] bit NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Attorney]') AND name = 'IsAprovador')
BEGIN
    ALTER TABLE [Attorney] ADD [IsAprovador] bit NOT NULL DEFAULT 0;
END

-- Adicionar campo EmAprovacao na tabela ProcessRecord
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcessRecord]') AND name = 'EmAprovacao')
BEGIN
    ALTER TABLE [ProcessRecord] ADD [EmAprovacao] bit NOT NULL DEFAULT 0;
END

-- Criar tabela LoteAprovacao
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoteAprovacao')
BEGIN
    CREATE TABLE [LoteAprovacao] (
        [Id] int NOT NULL IDENTITY(1,1),
        [DataCriacao] datetime2 NOT NULL,
        [CriadoPorId] int NOT NULL,
        [ClienteId] int NOT NULL,
        [PeriodoInicio] datetime2 NOT NULL,
        [PeriodoFim] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [TotalHoras] float NOT NULL,
        [ValorEstimado] float NOT NULL,
        [DataAprovacao] datetime2 NULL,
        [AprovadoPorId] int NULL,
        [ComentarioAprovador] nvarchar(max) NULL,
        [DataFaturamento] datetime2 NULL,
        [FaturadoPorId] int NULL,
        CONSTRAINT [PK_LoteAprovacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoteAprovacao_Attorney_AprovadoPorId] FOREIGN KEY ([AprovadoPorId]) REFERENCES [Attorney] ([Id]),
        CONSTRAINT [FK_LoteAprovacao_Attorney_CriadoPorId] FOREIGN KEY ([CriadoPorId]) REFERENCES [Attorney] ([Id]),
        CONSTRAINT [FK_LoteAprovacao_Attorney_FaturadoPorId] FOREIGN KEY ([FaturadoPorId]) REFERENCES [Attorney] ([Id]),
        CONSTRAINT [FK_LoteAprovacao_Client_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Client] ([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_LoteAprovacao_AprovadoPorId] ON [LoteAprovacao] ([AprovadoPorId]);
    CREATE INDEX [IX_LoteAprovacao_ClienteId] ON [LoteAprovacao] ([ClienteId]);
    CREATE INDEX [IX_LoteAprovacao_CriadoPorId] ON [LoteAprovacao] ([CriadoPorId]);
    CREATE INDEX [IX_LoteAprovacao_FaturadoPorId] ON [LoteAprovacao] ([FaturadoPorId]);
END

-- Criar tabela LoteAprovacaoItem
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoteAprovacaoItem')
BEGIN
    CREATE TABLE [LoteAprovacaoItem] (
        [Id] int NOT NULL IDENTITY(1,1),
        [LoteAprovacaoId] int NOT NULL,
        [ProcessRecordId] int NOT NULL,
        [Status] int NOT NULL,
        [Abonado] bit NOT NULL,
        [DataRevisao] datetime2 NULL,
        [ObservacaoRevisao] nvarchar(max) NULL,
        [FoiEditado] bit NOT NULL,
        [DescricaoOriginal] nvarchar(max) NULL,
        [HoraInicialOriginal] time NULL,
        [HoraFinalOriginal] time NULL,
        CONSTRAINT [PK_LoteAprovacaoItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoteAprovacaoItem_LoteAprovacao_LoteAprovacaoId] FOREIGN KEY ([LoteAprovacaoId]) REFERENCES [LoteAprovacao] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LoteAprovacaoItem_ProcessRecord_ProcessRecordId] FOREIGN KEY ([ProcessRecordId]) REFERENCES [ProcessRecord] ([Id])
    );
    
    CREATE INDEX [IX_LoteAprovacaoItem_LoteAprovacaoId] ON [LoteAprovacaoItem] ([LoteAprovacaoId]);
    CREATE INDEX [IX_LoteAprovacaoItem_ProcessRecordId] ON [LoteAprovacaoItem] ([ProcessRecordId]);
END

-- Criar tabela HistoricoAprovacao
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HistoricoAprovacao')
BEGIN
    CREATE TABLE [HistoricoAprovacao] (
        [Id] int NOT NULL IDENTITY(1,1),
        [LoteAprovacaoId] int NOT NULL,
        [DataHora] datetime2 NOT NULL,
        [UsuarioId] int NOT NULL,
        [TipoAcao] nvarchar(100) NOT NULL,
        [Detalhes] nvarchar(2000) NULL,
        [ProcessRecordId] int NULL,
        CONSTRAINT [PK_HistoricoAprovacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistoricoAprovacao_Attorney_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Attorney] ([Id]),
        CONSTRAINT [FK_HistoricoAprovacao_LoteAprovacao_LoteAprovacaoId] FOREIGN KEY ([LoteAprovacaoId]) REFERENCES [LoteAprovacao] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_HistoricoAprovacao_ProcessRecord_ProcessRecordId] FOREIGN KEY ([ProcessRecordId]) REFERENCES [ProcessRecord] ([Id])
    );
    
    CREATE INDEX [IX_HistoricoAprovacao_LoteAprovacaoId] ON [HistoricoAprovacao] ([LoteAprovacaoId]);
    CREATE INDEX [IX_HistoricoAprovacao_ProcessRecordId] ON [HistoricoAprovacao] ([ProcessRecordId]);
    CREATE INDEX [IX_HistoricoAprovacao_UsuarioId] ON [HistoricoAprovacao] ([UsuarioId]);
END

-- Criar tabela NotificacaoAprovacao
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificacaoAprovacao')
BEGIN
    CREATE TABLE [NotificacaoAprovacao] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UsuarioId] int NOT NULL,
        [LoteAprovacaoId] int NOT NULL,
        [TipoNotificacao] nvarchar(50) NOT NULL,
        [Mensagem] nvarchar(500) NOT NULL,
        [DataCriacao] datetime2 NOT NULL,
        [Lida] bit NOT NULL,
        [DataLeitura] datetime2 NULL,
        CONSTRAINT [PK_NotificacaoAprovacao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NotificacaoAprovacao_Attorney_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Attorney] ([Id]),
        CONSTRAINT [FK_NotificacaoAprovacao_LoteAprovacao_LoteAprovacaoId] FOREIGN KEY ([LoteAprovacaoId]) REFERENCES [LoteAprovacao] ([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_NotificacaoAprovacao_LoteAprovacaoId] ON [NotificacaoAprovacao] ([LoteAprovacaoId]);
    CREATE INDEX [IX_NotificacaoAprovacao_UsuarioId] ON [NotificacaoAprovacao] ([UsuarioId]);
END

-- Registrar a migration no histórico
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260509210000_AddFluxoAprovacaoFields')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260509210000_AddFluxoAprovacaoFields', '6.0.35');
END

PRINT 'Migration aplicada com sucesso!';
