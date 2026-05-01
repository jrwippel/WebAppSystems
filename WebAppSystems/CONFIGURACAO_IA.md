# 🤖 Guia de Configuração da IA

## Classes Principais

### 1. **Models/AIConfiguration.cs** ✅
Modelo que representa a configuração no banco de dados.

**Campos:**
- `Id` - Identificador único
- `Provider` - Provedor (GoogleGemini, OpenAI, Anthropic)
- `ApiKey` - Chave da API
- `Model` - Modelo a ser usado (gemini-1.5-flash-latest)
- `IsActive` - Se a IA está ativa
- `CreatedAt` - Data de criação
- `UpdatedAt` - Data de atualização

### 2. **Services/AIService.cs** ✅
Serviço principal que faz a integração com a API usando SDK oficial.

**Métodos principais:**
- `IsConfiguredAsync()` - Verifica se a IA está configurada
- `GenerateContentAsync()` - Gera conteúdo usando a IA
- `CallGoogleGeminiAsync()` - Chama a API do Google Gemini

### 3. **Services/DocumentAIAnalysisService.cs** ✅ (NOVO)
Serviço específico para análise de documentos jurídicos.

**Método principal:**
- `AnalyzeDocumentAsync()` - Analisa documento e retorna informações estruturadas

### 4. **Controllers/AIConfigurationController.cs** ✅
Controller para gerenciar a configuração da IA via interface web.

**Ações:**
- `Index` - Exibe formulário de configuração
- `Save` - Salva configuração no banco
- `TestConnection` - Testa conexão com a API
- `Debug` - Mostra informações de debug

## Como Configurar

### Opção 1: Via Interface Web (Recomendado)

1. Acesse: `/AIConfiguration/Index`
2. Preencha os campos:
   - **Provider**: GoogleGemini
   - **Model**: gemini-1.5-flash-latest
   - **API Key**: Sua chave do Google AI
   - **IsActive**: ✅ Marcado
3. Clique em "Salvar"

### Opção 2: Via SQL (Direto no Banco)

Execute o script: `Scripts/VerificarConfiguracaoIA.sql`

```sql
-- Inserir ou atualizar configuração
UPDATE AIConfiguration
SET 
    Provider = 'GoogleGemini',
    ApiKey = 'SUA_CHAVE_AQUI',
    Model = 'gemini-1.5-flash-latest',
    IsActive = 1,
    UpdatedAt = GETDATE()
WHERE Id = 1;
```

### Opção 3: Via appsettings.json (Backup)

O sistema ainda lê do `appsettings.json` como fallback:

```json
{
  "GoogleAI": {
    "ApiKey": "AIzaSyCssYAmEqvpYpGKZM9flnFFE0IgB9IoM2E"
  }
}
```

## Verificar se está funcionando

### 1. Verificar no banco de dados:
```sql
SELECT * FROM AIConfiguration;
```

### 2. Acessar endpoint de debug:
```
GET /AIConfiguration/Debug
```

### 3. Testar análise de documento:
1. Acesse: `/DocumentAnalysis/Index`
2. Faça upload de um documento PDF/DOCX
3. Aguarde a análise

## Problemas Comuns

### ❌ "IA não configurada"
**Solução:** Verifique se `IsActive = 1` no banco de dados

### ❌ "API Key não configurada"
**Solução:** Verifique se o campo `ApiKey` está preenchido

### ❌ "Erro na API do Google AI: 400"
**Solução:** Verifique se a API Key é válida e se o modelo está correto

### ❌ "Modelo não encontrado"
**Solução:** Use `gemini-1.5-flash-latest` ou `gemini-1.5-pro-latest`

## Modelos Disponíveis

### Google Gemini:
- ✅ `gemini-1.5-flash-latest` (Recomendado - Rápido e eficiente)
- ✅ `gemini-1.5-pro-latest` (Mais poderoso, mais lento)
- ❌ `gemini-2.5-flash` (Não existe)
- ❌ `gemini-pro` (Descontinuado)

### OpenAI (se configurar):
- `gpt-4`
- `gpt-4-turbo`
- `gpt-3.5-turbo`

### Anthropic (se configurar):
- `claude-3-sonnet`
- `claude-3-opus`

## Fluxo de Funcionamento

```
1. Usuário faz upload de documento
   ↓
2. DocumentAnalysisController.Upload()
   ↓
3. ProcessDocumentAnalysisInBackground()
   ↓
4. DocumentAIAnalysisService.AnalyzeDocumentAsync()
   ↓
5. AIService.GenerateContentAsync()
   ↓
6. AIService.CallGoogleGeminiAsync()
   ↓
7. Google Gemini API (SDK oficial)
   ↓
8. Resposta parseada e salva no banco
```

## Logs para Debug

Os logs aparecem no console da aplicação:

```
[AIService] Chamando Google Gemini SDK: modelo=gemini-1.5-flash-latest
[DocumentAI] Iniciando análise do documento: arquivo.pdf
[DocumentAI] Tamanho do texto: 5000 caracteres
[DocumentAI] Resposta recebida: 1200 caracteres
[DocumentAI] Análise parseada - LegalArea: Trabalhista
```

## Checklist de Configuração

- [ ] Tabela `AIConfiguration` existe no banco
- [ ] Registro existe na tabela com `Id = 1`
- [ ] Campo `Provider` = "GoogleGemini"
- [ ] Campo `Model` = "gemini-1.5-flash-latest"
- [ ] Campo `ApiKey` está preenchido
- [ ] Campo `IsActive` = 1 (true)
- [ ] Serviço `DocumentAIAnalysisService` registrado no `Program.cs`
- [ ] API Key do Google AI é válida

## Obter API Key do Google

1. Acesse: https://makersuite.google.com/app/apikey
2. Clique em "Create API Key"
3. Copie a chave gerada
4. Cole no campo `ApiKey` da configuração
