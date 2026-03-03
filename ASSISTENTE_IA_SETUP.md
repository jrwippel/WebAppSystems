# 🤖 Assistente Jurídico IA - Guia de Configuração

## ✅ O que já foi implementado:

### 1. Models
- ✅ `DocumentAnalysis.cs` - Modelo principal para armazenar análises
- ✅ `DocumentAnalysisViewModel.cs` - ViewModels para exibição

### 2. Services
- ✅ `AIDocumentAnalysisService.cs` - Integração com Google Gemini API
- ✅ `DocumentTextExtractorService.cs` - Extração de texto de PDF/DOCX
- ✅ `AttorneyRecommendationService.cs` - Recomendação inteligente de advogados

### 3. Controller
- ✅ `DocumentAnalysisController.cs` - Gerencia upload, análise e histórico

### 4. Views
- ✅ `Index.cshtml` - Tela de upload com drag & drop

### 5. Configurações
- ✅ Services registrados no `Program.cs`
- ✅ DbSet adicionado ao `WebAppSystemsContext.cs`
- ✅ Pacotes NuGet instalados:
  - itext7 (8.0.2) - Para leitura de PDFs
  - DocumentFormat.OpenXml (3.0.1) - Para leitura de DOCX

## ⚠️ O que FALTA fazer:

### 1. Migration do Banco de Dados
**IMPORTANTE:** A tabela `DocumentAnalysis` precisa ser criada no banco.

Execute no terminal:
```bash
dotnet ef migrations add AddDocumentAnalysis --project WebAppSystems/WebAppSystems.csproj
dotnet ef database update --project WebAppSystems/WebAppSystems.csproj
```

Se der erro, crie a migration manualmente ou execute o SQL direto no banco.

### 2. Configurar API Key do Google Gemini

**Passo 1:** Criar conta gratuita
- Acesse: https://aistudio.google.com/app/apikey
- Faça login com conta Google
- Clique em "Get API Key" → "Create API Key"
- Copie a chave gerada

**Passo 2:** Adicionar no `appsettings.json`
```json
{
  "GoogleAI": {
    "ApiKey": "SUA_API_KEY_AQUI"
  }
}
```

**Passo 3:** Adicionar no `appsettings.Development.json` também

### 3. Criar Views Restantes

Faltam 2 views:

**A) Details.cshtml** - Exibe análise completa do documento
- Resumo executivo
- Partes envolvidas
- Fundamentos legais
- Recomendações de advogados (top 3)
- Botão para atribuir advogado

**B) History.cshtml** - Lista histórico de análises
- Tabela com todos os documentos analisados
- Filtros por data, cliente, status
- Link para ver detalhes

### 4. Adicionar Item no Menu

Editar `WebAppSystems/Views/Shared/Components/Menu/Default.cshtml`:

Adicionar após o item "Calendário":
```html
<li class="nav-item">
    <a class="nav-link" asp-controller="DocumentAnalysis" asp-action="Index">
        <i class="bi bi-robot"></i>
        <span data-i18n="menu.assistente">Assistente IA</span>
    </a>
</li>
```

### 5. Criar Pasta para Uploads

Criar pasta para armazenar documentos:
```bash
mkdir WebAppSystems/wwwroot/uploads/documents
```

Ou adicionar no `.gitignore`:
```
wwwroot/uploads/documents/*
!wwwroot/uploads/documents/.gitkeep
```

### 6. Testar Funcionalidade

1. Rodar aplicação
2. Acessar `/DocumentAnalysis`
3. Fazer upload de um PDF ou DOCX
4. Aguardar análise (10-30 segundos)
5. Ver resultado com recomendações

## 🎯 Funcionalidades Implementadas:

### Upload e Análise
- ✅ Drag & drop de arquivos
- ✅ Suporte a PDF, DOCX, TXT
- ✅ Validação de tamanho (máx 10MB)
- ✅ Extração automática de texto
- ✅ Análise por IA (Google Gemini)

### Análise Inteligente
- ✅ Resumo executivo
- ✅ Identificação de área do direito
- ✅ Tipo de ação
- ✅ Complexidade (simples/média/alta)
- ✅ Estimativa de horas
- ✅ Extração de partes envolvidas
- ✅ Fundamentos legais citados
- ✅ Prazos identificados
- ✅ Valor da causa

### Recomendação de Advogados
- ✅ Algoritmo de matching inteligente
- ✅ Score de compatibilidade (0-100%)
- ✅ Top 3 advogados recomendados
- ✅ Justificativa detalhada
- ✅ Baseado em:
  - Especialização na área
  - Experiência em casos similares
  - Eficiência (tempo médio)
  - Disponibilidade atual
  - Complexidade vs senioridade

### Interface Moderna
- ✅ Design consistente com o resto do sistema
- ✅ Gradiente roxo/azul
- ✅ Responsivo para mobile
- ✅ Feedback visual (loading, progress)
- ✅ Polling automático do status

## 💰 Custos

### Google Gemini API
- **Tier Gratuito:** 1 milhão de tokens/mês
- **1 petição média:** ~5.000 tokens
- **= 200 análises/mês GRÁTIS**
- **Depois:** ~$0.50 por 1M tokens (muito barato)

### Exemplo Real:
- 50 petições/mês = ~250k tokens = **$0.00** (grátis)
- 500 petições/mês = ~2.5M tokens = **$1.25/mês**

## 🔒 Segurança

### Dados Sensíveis
- ⚠️ Documentos são enviados para API externa (Google)
- ✅ Use apenas para análise preliminar
- ✅ Não envie dados ultra-confidenciais
- ✅ Considere anonimizar informações sensíveis

### Alternativa Local (Futuro)
- Implementar Ollama para IA 100% local
- Sem envio de dados externos
- Requer servidor com GPU (opcional)

## 📝 Próximos Passos Recomendados

1. ✅ Finalizar views (Details e History)
2. ✅ Adicionar no menu
3. ✅ Testar com documentos reais
4. ✅ Ajustar prompts da IA se necessário
5. ✅ Adicionar filtros no histórico
6. ✅ Implementar exportação de análises em PDF
7. ✅ Adicionar notificações quando análise concluir
8. ✅ Implementar fila de processamento (para muitos uploads)

## 🐛 Troubleshooting

### Erro: "API Key não configurada"
- Verifique se adicionou a chave no appsettings.json
- Formato: `"GoogleAI": { "ApiKey": "sua-chave" }`

### Erro: "Tabela DocumentAnalysis não existe"
- Execute a migration: `dotnet ef database update`

### Erro ao extrair texto de PDF
- Verifique se o PDF não está protegido/criptografado
- PDFs escaneados precisam de OCR (não implementado ainda)

### Análise demora muito
- Normal: 10-30 segundos
- Se > 1 minuto, verifique conexão com API
- Verifique logs do console

## 📚 Documentação Adicional

- Google Gemini API: https://ai.google.dev/docs
- iText7 PDF: https://itextpdf.com/
- OpenXML SDK: https://github.com/dotnet/Open-XML-SDK

---

**Status:** Implementação 80% completa
**Falta:** Migration + Views Details/History + Item no menu
**Tempo estimado para finalizar:** 1-2 horas
