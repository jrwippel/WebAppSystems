-- Adiciona coluna ValorHoraVirtual na tabela Mensalista
-- Execute este script no banco de dados antes de usar o painel de Rentabilidade

IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Mensalista' AND COLUMN_NAME = 'ValorHoraVirtual'
)
BEGIN
    ALTER TABLE Mensalista ADD ValorHoraVirtual DECIMAL(18,2) NULL;
    PRINT 'Coluna ValorHoraVirtual adicionada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Coluna ValorHoraVirtual já existe.';
END
