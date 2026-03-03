# Correção TimeTracker - Passo a Passo

## Problema
TimeTracker não está listando os registros do usuário.

## Solução Aplicada
Atualizado o método `GetRecordsForToday` para:
- Remover filtro de "apenas hoje"
- Adicionar paginação (20 registros por vez)
- Buscar TODOS os registros do usuário
- Retornar dados no formato JSON correto

## Passos para Deploy

### 1. Parar a Aplicação
```bash
# Pare o IIS Express ou o processo do Visual Studio
```

### 2. Limpar Build Anterior
No Visual Studio:
- Build > Clean Solution
- Aguarde finalizar

### 3. Rebuild Completo
No Visual Studio:
- Build > Rebuild Solution
- Aguarde finalizar (pode demorar alguns minutos)

### 4. Verificar Arquivos Atualizados
Confirme que os arquivos foram atualizados:

**TimeTrackerController.cs** - Linha ~344:
```csharp
[HttpGet]
public async Task<IActionResult> GetRecordsForToday(int attorneyId, int page = 1, int pageSize = 20, string search = "")
{
    // Query base - mesma lógica do ProcessRecordsController
    var query = _context.ProcessRecord
        .Include(r => r.Client)
        .Where(r => r.AttorneyId == attorneyId && 
                   r.HoraFinal != null && 
                   r.HoraFinal != TimeSpan.Zero)
        .AsQueryable();
    // ... resto do código
}
```

**TimeTracker/Index.cshtml** - Linha ~1280:
```javascript
async function loadRecordsForToday(attorneyId) {
    try {
        const searchValue = document.getElementById("searchInput").value;
        const url = `/TimeTracker/GetRecordsForToday?attorneyId=${attorneyId}&page=${currentPage}&pageSize=20&search=${encodeURIComponent(searchValue)}`;
        console.log('Fetching records from:', url);
        // ... resto do código
    }
}
```

### 5. Iniciar Aplicação
- Pressione F5 ou clique em "Start"
- Aguarde a aplicação iniciar

### 6. Limpar Cache do Navegador
**IMPORTANTE**: O navegador pode estar usando cache antigo

**Chrome/Edge:**
1. Abra DevTools (F12)
2. Clique com botão direito no botão Refresh
3. Selecione "Empty Cache and Hard Reload"

OU

1. Ctrl+Shift+Delete
2. Selecione "Cached images and files"
3. Clique em "Clear data"

### 7. Testar
1. Acesse https://localhost:5095/TimeTracker
2. Abra o Console do navegador (F12 > Console)
3. Procure pelos logs:
   - "Fetching records from: /TimeTracker/GetRecordsForToday?..."
   - "Response status: 200"
   - "Data received: {records: [...], ...}"

### 8. Verificar Resultado Esperado
- Deve listar TODOS os registros do usuário logado
- Não apenas os de hoje
- Com paginação (20 por página)
- Ordenados por data (mais recente primeiro)

## Se Ainda Não Funcionar

### Verificar no Console do Navegador:
```javascript
// Cole isso no console e me envie o resultado:
fetch('/TimeTracker/GetRecordsForToday?attorneyId=1&page=1&pageSize=20&search=')
  .then(r => r.json())
  .then(d => console.log('Response:', d))
  .catch(e => console.error('Error:', e))
```

### Verificar Logs do Backend:
Procure no Output do Visual Studio por:
```
SELECT [p].[Id], [p].[AttorneyId], [p].[ClientId], ...
FROM [ProcessRecord] AS [p]
INNER JOIN [Client] AS [c] ON [p].[ClientId] = [c].[Id]
WHERE ([p].[AttorneyId] = @__attorneyId_0) AND ([p].[HoraFinal] <> '00:00:00')
```

Se essa query aparecer, significa que o backend está funcionando.

## Arquivos Modificados
- `WebAppSystems/Controllers/TimeTrackerController.cs`
- `WebAppSystems/Views/TimeTracker/Index.cshtml`

## Backup
Se precisar reverter, os arquivos originais estavam filtrando apenas registros de hoje.
