# 📦 Commit Seletivo - Apenas Telas Redesenhadas

## Objetivo
Publicar em produção apenas as melhorias visuais das 30 telas e correção do cronômetro, SEM o Assistente IA.

---

## 🎯 Arquivos para Commitar

### Correção do Cronômetro
```bash
git add WebAppSystems/Views/Shared/_Layout.cshtml
git add WebAppSystems/Views/TimeTracker/Index.cshtml
git add WebAppSystems/Controllers/TimeTrackerController.cs
```

### Otimização de Performance
```bash
git add WebAppSystems/Controllers/ProcessRecordsController.cs
```

### 30 Telas Redesenhadas
```bash
# Attorneys
git add WebAppSystems/Views/Attorneys/Index.cshtml
git add WebAppSystems/Views/Attorneys/Details.cshtml
git add WebAppSystems/Views/Attorneys/Edit.cshtml
git add WebAppSystems/Views/Attorneys/Delete.cshtml

# Clients
git add WebAppSystems/Views/Clients/Index.cshtml
git add WebAppSystems/Views/Clients/Details.cshtml
git add WebAppSystems/Views/Clients/Edit.cshtml
git add WebAppSystems/Views/Clients/Delete.cshtml

# Departments
git add WebAppSystems/Views/Departments/Index.cshtml
git add WebAppSystems/Views/Departments/Details.cshtml
git add WebAppSystems/Views/Departments/Edit.cshtml
git add WebAppSystems/Views/Departments/Delete.cshtml

# ProcessRecords (Atividades)
git add WebAppSystems/Views/ProcessRecords/Index.cshtml
git add WebAppSystems/Views/ProcessRecords/Details.cshtml
git add WebAppSystems/Views/ProcessRecords/Edit.cshtml
git add WebAppSystems/Views/ProcessRecords/Delete.cshtml

# ValorClientes
git add WebAppSystems/Views/ValorClientes/Index.cshtml
git add WebAppSystems/Views/ValorClientes/Details.cshtml
git add WebAppSystems/Views/ValorClientes/Edit.cshtml
git add WebAppSystems/Views/ValorClientes/Delete.cshtml

# Mensalistas
git add WebAppSystems/Views/Mensalistas/Index.cshtml
git add WebAppSystems/Views/Mensalistas/Details.cshtml
git add WebAppSystems/Views/Mensalistas/Edit.cshtml
git add WebAppSystems/Views/Mensalistas/Delete.cshtml

# Mensalista (Relatórios)
git add WebAppSystems/Views/Mensalista/Index.cshtml
git add WebAppSystems/Views/Mensalista/SimpleSearch.cshtml
git add WebAppSystems/Views/Mensalista/ResultadoMes.cshtml
git add WebAppSystems/Views/Mensalista/ResultadoMedia.cshtml
git add WebAppSystems/Views/Mensalista/ResultadoAcumulado.cshtml

# PercentualAreas
git add WebAppSystems/Views/PercentualAreas/Index.cshtml
git add WebAppSystems/Views/PercentualAreas/Details.cshtml
git add WebAppSystems/Views/PercentualAreas/Edit.cshtml
git add WebAppSystems/Views/PercentualAreas/Delete.cshtml
git add WebAppSystems/Views/PercentualAreas/Create.cshtml

# Outras telas
git add WebAppSystems/Views/Home/Index.cshtml
git add WebAppSystems/Views/AlterarSenha/Index.cshtml
git add WebAppSystems/Views/Parametros/Index.cshtml
git add WebAppSystems/Views/Calendar/Index.cshtml
```

---

## 🚫 Arquivos para NÃO Commitar (Assistente IA)

**NÃO adicione estes arquivos:**
- `WebAppSystems/Models/DocumentAnalysis.cs`
- `WebAppSystems/Models/ViewModels/DocumentAnalysisViewModel.cs`
- `WebAppSystems/Services/AIDocumentAnalysisService.cs`
- `WebAppSystems/Services/DocumentTextExtractorService.cs`
- `WebAppSystems/Services/AttorneyRecommendationService.cs`
- `WebAppSystems/Controllers/DocumentAnalysisController.cs`
- `WebAppSystems/Views/DocumentAnalysis/*`
- `WebAppSystems/Migrations/*AddDocumentAnalysis*`
- Alterações em `Program.cs` (services do IA)
- Alterações em `WebAppSystemsContext.cs` (DbSet DocumentAnalysis)

---

## 📝 Comandos para Executar

### 1. Verificar status atual
```bash
git status
```

### 2. Adicionar apenas os arquivos das telas
Execute o script acima (seção "Arquivos para Commitar")

### 3. Verificar o que será commitado
```bash
git status
```

### 4. Fazer commit
```bash
git commit -m "feat: Redesign de 30 telas + correção cronômetro + otimização performance

- Redesign completo de 30 telas com padrão moderno
- Header gradiente roxo/azul (#667eea, #764ba2)
- Cards brancos com sombras suaves
- Ícones Bootstrap Icons
- Delete com header vermelho
- Todas as telas responsivas para mobile
- Correção do bug do cronômetro mostrando 24h
- Otimização de performance na tela de Atividades"
```

### 5. Push para produção
```bash
git push origin main
```

---

## ✅ Vantagens desta Abordagem

1. **Código do IA permanece local** - Você pode continuar testando
2. **Produção recebe apenas melhorias visuais** - Sem riscos
3. **Fácil de publicar IA depois** - Quando estiver pronto, só commitar o resto
4. **Sem rollback necessário** - Não precisa desfazer nada

---

## 🔍 Verificação Final

Antes de fazer push, verifique:

```bash
# Ver arquivos que serão commitados
git diff --cached --name-only

# Garantir que NÃO aparecem:
# - DocumentAnalysis
# - AIDocumentAnalysisService
# - DocumentTextExtractorService
# - AttorneyRecommendationService
```

---

## 💡 Quando Publicar o Assistente IA

Quando estiver pronto para publicar o IA:

```bash
# Adicionar todos os arquivos do IA
git add WebAppSystems/Models/DocumentAnalysis.cs
git add WebAppSystems/Models/ViewModels/DocumentAnalysisViewModel.cs
git add WebAppSystems/Services/AIDocumentAnalysisService.cs
git add WebAppSystems/Services/DocumentTextExtractorService.cs
git add WebAppSystems/Services/AttorneyRecommendationService.cs
git add WebAppSystems/Controllers/DocumentAnalysisController.cs
git add WebAppSystems/Views/DocumentAnalysis/
git add WebAppSystems/Migrations/*AddDocumentAnalysis*
git add WebAppSystems/Data/WebAppSystemsContext.cs
git add WebAppSystems/Program.cs

# Commit
git commit -m "feat: Adiciona Assistente Jurídico IA com Google Gemini"

# Push
git push origin main
```

---

**Tempo estimado:** 5 minutos
**Risco:** Zero (só publica o que está funcionando)
