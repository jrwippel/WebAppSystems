-- Script para definir um usuário como Financeiro
-- Substitua 'SEU_LOGIN' pelo login do usuário que você está usando

-- Listar todos os usuários para você escolher
SELECT Id, Name, Login, Email, Perfil, IsFinanceiro, IsAprovador, Inativo
FROM [dbo].[Attorney]
ORDER BY Name;

-- Descomente e ajuste a linha abaixo para definir o usuário como Financeiro
-- UPDATE [dbo].[Attorney] SET IsFinanceiro = 1, IsAprovador = 0 WHERE Login = 'SEU_LOGIN';

-- Ou use o ID do usuário:
-- UPDATE [dbo].[Attorney] SET IsFinanceiro = 1, IsAprovador = 0 WHERE Id = 1;
