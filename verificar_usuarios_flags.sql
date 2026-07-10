-- Verificar flags dos usuários
SELECT Id, Name, Login, Perfil, IsFinanceiro, IsAprovador, Inativo
FROM Attorney
WHERE Id IN (1, 2, 6)
ORDER BY Id;

-- Atualizar flags se necessário
-- Usuário Financeiro (ID=1)
UPDATE Attorney SET IsFinanceiro = 1, IsAprovador = 0 WHERE Id = 1;

-- Usuário Jackson Wippel (ID=2) 
UPDATE Attorney SET IsFinanceiro = 1, IsAprovador = 0 WHERE Id = 2;

-- Usuário Aprovador (ID=6)
UPDATE Attorney SET IsFinanceiro = 0, IsAprovador = 1 WHERE Id = 6;

-- Verificar novamente após atualização
SELECT Id, Name, Login, Perfil, IsFinanceiro, IsAprovador, Inativo
FROM Attorney
WHERE Id IN (1, 2, 6)
ORDER BY Id;
