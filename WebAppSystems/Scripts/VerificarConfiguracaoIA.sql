-- Script para verificar e configurar a IA no banco de dados

-- 1. VERIFICAR se a tabela AIConfiguration existe
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'AIConfiguration';

-- 2. VERIFICAR configuração atual
SELECT * FROM AIConfiguration;

-- 3. CORRIGIR O MODELO (IMPORTANTE!)
-- O modelo correto é 'gemini-1.5-flash' (sem -latest)
UPDATE AIConfiguration
SET 
    Provider = 'GoogleGemini',
    Model = 'gemini-1.5-flash',  -- SEM -latest!
    IsActive = 1,
    UpdatedAt = GETDATE()
WHERE Id = 1;

-- 4. INSERIR configuração inicial (se não existir)
-- Execute apenas se não houver nenhum registro
IF NOT EXISTS (SELECT 1 FROM AIConfiguration)
BEGIN
    INSERT INTO AIConfiguration (Provider, ApiKey, Model, IsActive, CreatedAt)
    VALUES ('GoogleGemini', 'AIzaSyCssYAmEqvpYpGKZM9flnFFE0IgB9IoM2E', 'gemini-1.5-flash', 1, GETDATE());
    
    PRINT 'Configuração inicial criada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Configuração atualizada. Verifique abaixo:';
END

-- 5. VERIFICAR novamente após alterações
SELECT 
    Id,
    Provider,
    Model,
    CASE 
        WHEN LEN(ApiKey) > 0 THEN 'Configurada (' + CAST(LEN(ApiKey) AS VARCHAR) + ' caracteres)'
        ELSE 'NÃO CONFIGURADA'
    END AS ApiKeyStatus,
    IsActive,
    CreatedAt,
    UpdatedAt
FROM AIConfiguration;

-- 6. MODELOS VÁLIDOS:
-- ✅ gemini-1.5-flash (Recomendado)
-- ✅ gemini-1.5-pro
-- ❌ gemini-1.5-flash-latest (NÃO FUNCIONA)
-- ❌ gemini-2.5-flash (NÃO EXISTE)
