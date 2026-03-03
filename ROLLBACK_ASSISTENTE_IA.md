# 🔄 Guia de Rollback Completo - Assistente IA

## ⚠️ Use este guia SE decidir remover o Assistente IA

Este guia garante remoção 100% de tudo relacionado ao Assistente IA.

---

## 🗑️ Passo 1: Reverter Migration do Banco

Se você executou a migration, precisa reverter:

```bash
# Ver migrations aplicadas
dotnet ef migrations list --project WebAppSystems/WebAppSystems.csproj

# Reverter para a migration anterior (antes de AddDocumentAnalysis)
dotnet ef database update <NOME_DA_MIGRATION_ANTERIOR> --project WebAppSystems/WebAppSystems.csproj

# Exemplo:
dotnet ef database update AlterTableAttorneyUserInativo --project WebAppSystems/WebAppSystems.csproj

# Remover a migration do código
dotnet ef migrations remove --project WebAppSystems/WebAppSystems.csproj
```

**OU via SQL direto:**
```sql
-- Dropar a tabela
DROP TABLE IF EXISTS DocumentAnalysis;

-- Remover registro da migration
DELETE FROM __EFMigrationsHistory WHERE MigrationId LIKE '%AddDocumentAnalysis%';
```

---

## 🗂️ Passo 2: Deletar Arquivos Criados

Execute no PowerShell:

```powershell
# Models
Remove-Item "WebAppSystems/Models/DocumentAnalysis.cs" -Force
Remove-Item "WebAppSystems/Models/ViewModels/DocumentAnalysisViewModel.cs" -Force

# Services
Remove-Item "WebAppSystems/Services/AIDocumentAnalysisService.cs" -Force
Remove-Item "WebAppSystems/Services/DocumentTextExtractorService.cs" -Force
Remove-Item "WebAppSystems/Services/AttorneyRecommendationService.cs" -Force

# Controller
Remove-Item "WebAppSystems/Controllers/DocumentAnalysisController.cs" -Force

# Views
Remove-Item "WebAppSystems/Views/DocumentAnalysis" -Recurse -Force

# Documentação
Remove-Item "ASSISTENTE_IA_SETUP.md" -Force
Remove-Item "ASSISTENTE_IA_RESUMO.md" -Force
Remove-Item "ROLLBACK_ASSISTENTE_IA.md" -Force

# Pasta de uploads (se criada)
Remove-Item "WebAppSystems/wwwroot/uploads/documents" -Recurse -Force -ErrorAction SilentlyContinue
```

---

## 📝 Passo 3: Reverter Program.cs

Remover estas linhas do `WebAppSystems/Program.cs`:

```csharp
// REMOVER ESTAS LINHAS:
builder.Services.AddScoped<DocumentTextExtractorService>();
builder.Services.AddScoped<AIDocumentAnalysisService>();
builder.Services.AddScoped<AttorneyRecommendationService>();
```

Localizar por volta da linha 45 e deletar as 3 linhas acima.

---

## 📝 Passo 4: Reverter WebAppSystemsContext.cs

Remover esta linha do `WebAppSystems/Data/WebAppSystemsContext.cs`:

```csharp
// REMOVER ESTA LINHA:
public DbSet<WebAppSystems.Models.DocumentAnalysis> DocumentAnalysis { get; set; } = default!;
```

---

## 📦 Passo 5: Remover Pacotes NuGet (Opcional)

Se quiser remover os pacotes instalados:

```bash
dotnet remove WebAppSystems/WebAppSystems.csproj package itext7
dotnet remove WebAppSystems/WebAppSystems.csproj package DocumentFormat.OpenXml
```

**Nota:** Estes pacotes não afetam o sistema se não forem usados.

---

## 🔧 Passo 6: Remover do Menu (Se adicionou)

Se adicionou item no menu, remover de `WebAppSystems/Views/Shared/Components/Menu/Default.cshtml`:

```html
<!-- REMOVER ESTE BLOCO: -->
<li class="nav-item">
    <a class="nav-link" asp-controller="DocumentAnalysis" asp-action="Index">
        <i class="bi bi-robot"></i>
        <span data-i18n="menu.assistente">Assistente IA</span>
    </a>
</li>
```

---

## 🔑 Passo 7: Remover API Key (Opcional)

Remover do `appsettings.json` e `appsettings.Development.json`:

```json
// REMOVER ESTE BLOCO:
"GoogleAI": {
  "ApiKey": "sua-chave-aqui"
}
```

---

## ✅ Passo 8: Verificar Remoção Completa

```bash
# Build para verificar se não há erros
dotnet build WebAppSystems/WebAppSystems.csproj

# Se tudo OK, commitar
git add .
git commit -m "revert: Remove Assistente Jurídico IA completamente"
git push
```

---

## 🔍 Checklist de Verificação

Após executar todos os passos, verificar:

- [ ] Tabela `DocumentAnalysis` não existe no banco
- [ ] Migration `AddDocumentAnalysis` removida
- [ ] Arquivos deletados (Models, Services, Controller, Views)
- [ ] Program.cs sem referências aos services
- [ ] DbContext sem DbSet de DocumentAnalysis
- [ ] Menu sem item "Assistente IA"
- [ ] Build sem erros
- [ ] Sistema roda normalmente

---

## 🎯 Resultado Final

Após seguir todos os passos:
- ✅ Banco de dados volta ao estado anterior
- ✅ Código limpo (sem arquivos do Assistente IA)
- ✅ Sistema funciona exatamente como antes
- ✅ Nenhum rastro do Assistente IA

---

## 💡 Dica

Se quiser manter o código mas desativar temporariamente:
1. Não adicione no menu
2. Não configure a API Key
3. O código fica lá, mas ninguém acessa

Assim pode reativar depois sem reinstalar tudo.

---

**Tempo estimado para rollback completo:** 10-15 minutos
**Dificuldade:** Fácil (seguir passo a passo)
