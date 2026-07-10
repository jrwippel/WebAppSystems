-- Script para atualizar permissões de usuário
-- Escolha UMA das opções abaixo descomentando a linha correspondente

-- Opção 1: Definir Jackson Wippel como Financeiro
UPDATE [dbo].[Attorney] SET IsFinanceiro = 1, IsAprovador = 0 WHERE Id = 2;

-- Opção 2: Definir Jaime Wippel como Financeiro
-- UPDATE [dbo].[Attorney] SET IsFinanceiro = 1, IsAprovador = 0 WHERE Id = 4;

-- Opção 3: Definir James Bond como Financeiro
-- UPDATE [dbo].[Attorney] SET IsFinanceiro = 1, IsAprovador = 0 WHERE Id = 3;

-- Opção 4: Definir Aprovador como Aprovador (já está configurado)
-- UPDATE [dbo].[Attorney] SET IsFinanceiro = 0, IsAprovador = 1 WHERE Id = 6;

-- Verificar as alterações
SELECT Id, Name, Login, Perfil, IsFinanceiro, IsAprovador
FROM [dbo].[Attorney]
WHERE Id IN (1, 2, 3, 4, 6)
ORDER BY Name;

PRINT 'Permissões atualizadas com sucesso!';
PRINT 'IMPORTANTE: Faça logout e login novamente para as alterações terem efeito!';
