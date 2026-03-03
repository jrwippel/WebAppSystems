# 🧪 Como Testar o Assistente Jurídico IA

## ✅ Pré-requisitos

Antes de testar, você precisa:

### 1. Configurar API Key do Google Gemini (OBRIGATÓRIO)

**Passo 1:** Criar conta e gerar API Key
- Acesse: https://aistudio.google.com/app/apikey
- Faça login com sua conta Google
- Clique em "Get API Key" → "Create API Key"
- Copie a chave gerada (começa com "AIza...")

**Passo 2:** Adicionar no `appsettings.json`

Abra `WebAppSystems/appsettings.json` e adicione:
```json
{
  "ConnectionStrings": {
    ...
  },
  "GoogleAI": {
    "ApiKey": "COLE_SUA_CHAVE_AQUI"
  },
  ...
}
```

**Passo 3:** Adicionar também no `appsettings.Development.json`

### 2. Executar Migration do Banco

```bash
# Criar a migration
dotnet ef migrations add AddDocumentAnalysis --project WebAppSystems/WebAppSystems.csproj

# Aplicar no banco
dotnet ef database update --project WebAppSystems/WebAppSystems.csproj
```

**OU** execute o SQL direto no banco (mais rápido):

```sql
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
    FOREIGN KEY ([UploadedByAttorneyId]) REFERENCES [Attorney]([Id]),
    FOREIGN KEY ([AssignedToAttorneyId]) REFERENCES [Attorney]([Id]),
    FOREIGN KEY ([ClientId]) REFERENCES [Client]([Id])
);
```

### 3. Criar Pasta de Uploads

```bash
mkdir WebAppSystems/wwwroot/uploads
mkdir WebAppSystems/wwwroot/uploads/documents
```

### 4. (Opcional) Adicionar no Menu

Edite `WebAppSystems/Views/Shared/Components/Menu/Default.cshtml`

Adicione após o item "Calendário":
```html
<li class="nav-item">
    <a class="nav-link" asp-controller="DocumentAnalysis" asp-action="Index">
        <i class="bi bi-robot"></i>
        <span>Assistente IA</span>
    </a>
</li>
```

---

## 🚀 Como Testar

### Teste 1: Upload e Análise Básica

1. **Rodar aplicação**
   ```bash
   dotnet run --project WebAppSystems/WebAppSystems.csproj
   ```

2. **Acessar diretamente**
   - URL: `https://localhost:PORTA/DocumentAnalysis`
   - Ou pelo menu (se adicionou)

3. **Fazer upload de um documento**
   - Arraste um PDF ou DOCX
   - Ou clique para selecionar
   - Tipos aceitos: PDF, DOCX, TXT
   - Tamanho máximo: 10 MB

4. **Aguardar análise**
   - Tempo: 10-30 segundos
   - Você verá uma barra de progresso
   - Será redirecionado automaticamente

5. **Ver resultado**
   - Resumo executivo
   - Área do direito identificada
   - Complexidade
   - Partes envolvidas
   - Fundamentos legais
   - Top 3 advogados recomendados

### Teste 2: Recomendação de Advogados

1. **Verificar recomendações**
   - Veja os 3 advogados sugeridos
   - Score de compatibilidade (0-100%)
   - Justificativa detalhada
   - Estatísticas (casos similares, tempo médio, disponibilidade)

2. **Atribuir advogado**
   - Clique em "Atribuir para [Nome]"
   - Confirme
   - Documento fica marcado como atribuído

### Teste 3: Histórico

1. **Acessar histórico**
   - Clique em "Ver Histórico"
   - Ou acesse: `/DocumentAnalysis/History`

2. **Verificar lista**
   - Todos os documentos analisados
   - Status de cada um
   - Quem está atribuído
   - Botão para ver detalhes

---

## 📄 Documentos de Teste

### Opção 1: Criar um PDF de teste

Crie um arquivo Word com este conteúdo e salve como PDF:

```
PETIÇÃO INICIAL
AÇÃO TRABALHISTA - RESCISÃO INDIRETA

EXCELENTÍSSIMO SENHOR DOUTOR JUIZ DE DIREITO DA VARA DO TRABALHO

JOÃO SILVA, brasileiro, solteiro, operário, portador do CPF nº 123.456.789-00,
residente e domiciliado na Rua das Flores, 123, São Paulo/SP, vem, por seu
advogado que esta subscreve, propor a presente

AÇÃO DE RESCISÃO INDIRETA

em face de EMPRESA XYZ LTDA, pessoa jurídica de direito privado, inscrita no
CNPJ sob nº 12.345.678/0001-90, com sede na Av. Paulista, 1000, São Paulo/SP,
pelos fatos e fundamentos jurídicos a seguir expostos:

DOS FATOS

O Reclamante foi admitido pela Reclamada em 01/01/2020, exercendo a função de
operário, com salário mensal de R$ 3.000,00.

Durante todo o período contratual, o Reclamante foi submetido a assédio moral
constante por parte de seu superior hierárquico, além de não receber o pagamento
de horas extras devidas.

DO DIREITO

A conduta da Reclamada configura falta grave, nos termos do art. 483 da CLT,
autorizando a rescisão indireta do contrato de trabalho.

O não pagamento de horas extras viola o art. 7º, XVI da Constituição Federal.

DOS PEDIDOS

Diante do exposto, requer:
a) A rescisão indireta do contrato de trabalho;
b) O pagamento de horas extras não pagas;
c) Indenização por danos morais no valor de R$ 50.000,00;
d) Demais verbas rescisórias.

Valor da causa: R$ 85.000,00

São Paulo, 02 de março de 2026.

[Assinatura do Advogado]
OAB/SP 123.456
```

### Opção 2: Usar documento real (anonimizado)

Se tiver um documento real, remova dados sensíveis antes de testar.

---

## 🔍 O que Observar

### ✅ Deve Funcionar:
- Upload de PDF/DOCX/TXT
- Extração de texto
- Análise por IA (10-30s)
- Resumo gerado
- Área identificada corretamente
- Complexidade avaliada
- Recomendação de 3 advogados
- Score de compatibilidade
- Atribuição funcionando
- Histórico listando documentos

### ⚠️ Possíveis Problemas:

**Erro: "API Key não configurada"**
- Solução: Adicione a chave no appsettings.json

**Erro: "Tabela DocumentAnalysis não existe"**
- Solução: Execute a migration ou SQL manual

**Análise demora muito (>1 min)**
- Verifique conexão com internet
- API do Google pode estar lenta
- Documento muito grande (>10MB)

**Recomendações vazias**
- Normal se não houver advogados cadastrados
- Ou se não houver histórico de ProcessRecords

**PDF não extrai texto**
- PDF pode estar protegido/criptografado
- PDF escaneado precisa OCR (não implementado)

---

## 📊 Métricas de Sucesso

Teste bem-sucedido se:
- ✅ Upload funciona
- ✅ Análise completa em <30s
- ✅ Resumo faz sentido
- ✅ Área identificada corretamente
- ✅ Recomendações aparecem (se houver advogados)
- ✅ Atribuição funciona
- ✅ Histórico lista documentos

---

## 🐛 Se Encontrar Bugs

1. Verifique logs do console
2. Verifique Network tab (F12)
3. Verifique tabela DocumentAnalysis no banco
4. Verifique se API Key está correta

---

## 💰 Monitorar Uso da API

- Acesse: https://aistudio.google.com/app/apikey
- Veja quantos tokens usou
- Limite gratuito: 1M tokens/mês
- 1 análise ≈ 5k tokens
- = 200 análises grátis/mês

---

## ✅ Checklist de Teste

- [ ] API Key configurada
- [ ] Migration executada
- [ ] Pasta de uploads criada
- [ ] Aplicação rodando
- [ ] Upload de PDF funciona
- [ ] Análise completa
- [ ] Resumo gerado
- [ ] Recomendações aparecem
- [ ] Atribuição funciona
- [ ] Histórico lista documentos
- [ ] Sem erros no console

---

## 🎯 Próximo Passo

Se tudo funcionar e você gostar:
- ✅ Commitar as alterações
- ✅ Adicionar no menu permanentemente
- ✅ Usar em produção

Se NÃO gostar:
- ❌ Seguir guia: `ROLLBACK_ASSISTENTE_IA.md`
- ❌ Reverter 100% (banco + código)

---

**Boa sorte com os testes!** 🚀

Se tiver dúvidas, consulte:
- `ASSISTENTE_IA_SETUP.md` - Guia completo
- `ASSISTENTE_IA_RESUMO.md` - Visão geral
- `ROLLBACK_ASSISTENTE_IA.md` - Como reverter
