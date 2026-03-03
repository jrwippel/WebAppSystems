-- Script para criar tabela DocumentAnalysis
-- Execute este script diretamente no SQL Server Management Studio ou Azure Data Studio

-- Verificar se a tabela já existe
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentAnalysis]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DocumentAnalysis](
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [FileName] NVARCHAR(255) NOT NULL,
        [FileType] NVARCHAR(50) NOT NULL,
        [FileSize] BIGINT NOT NULL,
        [FilePath] NVARCHAR(MAX) NOT NULL,
        [UploadDate] DATETIME2 NOT NULL,
        [UploadedByAttorneyId] INT NOT NULL,
        [Summary] NVARCHAR(MAX) NULL,
        [LegalArea] NVARCHAR(100) NULL,
        [ActionType] NVARCHAR(100) NULL,
        [Complexity] NVARCHAR(50) NULL,
        [EstimatedHours] INT NULL,
        [MainTopics] NVARCHAR(MAX) NULL,
        [LegalBasis] NVARCHAR(MAX) NULL,
        [Parties] NVARCHAR(MAX) NULL,
        [CauseValue] DECIMAL(18,2) NULL,
        [Deadlines] NVARCHAR(MAX) NULL,
        [RecommendedAttorneys] NVARCHAR(MAX) NULL,
        [AnalysisStatus] NVARCHAR(50) NULL,
        [AnalysisDate] DATETIME2 NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [AssignedToAttorneyId] INT NULL,
        [AssignedDate] DATETIME2 NULL,
        [ClientId] INT NULL,
        CONSTRAINT [FK_DocumentAnalysis_Attorney_UploadedBy] FOREIGN KEY ([UploadedByAttorneyId]) REFERENCES [Attorney]([Id]),
        CONSTRAINT [FK_DocumentAnalysis_Attorney_AssignedTo] FOREIGN KEY ([AssignedToAttorneyId]) REFERENCES [Attorney]([Id]),
        CONSTRAINT [FK_DocumentAnalysis_Client] FOREIGN KEY ([ClientId]) REFERENCES [Client]([Id])
    );
    
    PRINT 'Tabela DocumentAnalysis criada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Tabela DocumentAnalysis já existe.';
END
GO
