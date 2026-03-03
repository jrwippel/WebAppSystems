# 🚀 Guia Rápido - Testar Assistente IA

## ✅ Status Atual

- ✅ Código implementado (100%)
- ✅ Pacotes NuGet instalados (itext7, DocumentFormat.OpenXml)
- ✅ Services registrados no Program.cs
- ✅ DbSet adicionado no Context
- ✅ Pasta de uploads criada
- ⏳ **FALTA:** Criar tabela no banco + Configurar API Key

---

## 📋 Checklist Rápido (5 minutos)

### 1️⃣ Criar Tabela no Banco (2 minutos)

**Opção A - SQL Direto (RECOMENDADO):**
1. Abra SQL Server Management Studio ou Azure Data Studio
2. Conecte no seu banco de dados
3. Execute o script: `WebAppSystems/Scripts/AddDocumentAnalysisTable.sql`
4. Pronto! ✅

**Opção B - Via Migration (se preferir):**
```bash
# Só funciona se não houver erro de assembly
dotnet ef migrations add AddDocumentAnalysis --project WebAppSystems/WebAppSystems.csproj
dotnet ef database update --project WebAppSystems/WebAppSystems.csproj
```

### 2️⃣ Configurar API Key do Google Gemini (2 minutos)

1. Acesse: https://aistudio.google.com/app/apikey
2. Faça login com Google
3. Clique "Get API Key" → "Create API Key"
4. Copie a chave (começa com "AIza...")
5. Abra `WebAppSystems/appsettings.json`
6. Adicione no final (antes do último `}`):

```json
  "GoogleAI": {
    "ApiKey": "COLE_SUA_CHAVE_AQUI"
  }
```

7. Faça o mesmo em `WebAppSystems/appsettings.Development.json`

### 3️⃣ Rodar e Testar (1 minuto)

```bash
dotnet run --project WebAppSystems/WebAppSystems.csproj
```

Acesse: `https://localhost:PORTA/DocumentAnalysis`

---

## 🧪 Teste Básico

1. **Upload:** Arraste um PDF ou DOCX (máx 10MB)
2. **Aguarde:** 10-30 segundos (IA analisando)
3. **Veja:** Resumo + Área do direito + Top 3 advogados recomendados
4. **Atribua:** Clique em "Atribuir para [Nome]"
5. **Histórico:** Veja todos os documentos analisados

---

## 📄 Documento de Teste

Se não tiver um documento, crie um arquivo Word com este texto e salve como PDF:

```
PETIÇÃO INICIAL - AÇÃO TRABALHISTA

JOÃO SILVA, brasileiro, operário, CPF 123.456.789-00, vem propor

AÇÃO DE RESCISÃO INDIRETA

em face de EMPRESA XYZ LTDA, CNPJ 12.345.678/0001-90.

DOS FATOS:
O Reclamante foi admitido em 01/01/2020, com salário de R$ 3.000,00.
Foi submetido a assédio moral e não recebeu horas extras.

DO DIREITO:
Configura falta grave (art. 483 CLT).
Viola art. 7º, XVI da CF.

DOS PEDIDOS:
a) Rescisão indireta
b) Horas extras
c) Danos morais: R$ 50.000,00

Valor da causa: R$ 85.000,00
```

---

## ✅ O que Deve Funcionar

- Upload de PDF/DOCX/TXT
- Análise automática (10-30s)
- Resumo executivo gerado
- Área do direito identificada
- Complexidade avaliada
- Top 3 advogados recomendados com score
- Atribuição de advogado
- Histórico de análises

---

## ❌ Se NÃO Gostar

Execute o rollback completo:
1. Siga o guia: `ROLLBACK_ASSISTENTE_IA.md`
2. Remove 100% (tabela + código)
3. Sistema volta ao normal

---

## 💰 Custo

- **GRATUITO:** Até 200 análises/mês
- Depois: ~$1.25 por 500 análises
- Praticamente grátis!

---

## 🐛 Problemas Comuns

**"API Key não configurada"**
→ Adicione a chave no appsettings.json

**"Tabela não existe"**
→ Execute o SQL: `WebAppSystems/Scripts/AddDocumentAnalysisTable.sql`

**"Erro ao extrair texto do PDF"**
→ PDF pode estar protegido ou escaneado (precisa OCR)

**"Nenhum advogado recomendado"**
→ Normal se não houver advogados cadastrados ou histórico

---

## 📞 Mais Informações

- **Guia completo:** `TESTE_ASSISTENTE_IA.md`
- **Como funciona:** `ASSISTENTE_IA_RESUMO.md`
- **Configuração técnica:** `ASSISTENTE_IA_SETUP.md`
- **Como reverter:** `ROLLBACK_ASSISTENTE_IA.md`

---

**Pronto para testar?** 🚀

1. Execute o SQL
2. Configure a API Key
3. Rode a aplicação
4. Acesse `/DocumentAnalysis`
5. Faça upload de um documento

**Boa sorte!** 🎉
