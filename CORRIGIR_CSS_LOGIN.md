# Corrigir CSS da Tela de Login

## 🔍 Problema
Alguns usuários veem a tela de login sem estilo (apenas HTML puro).

## 🎯 Causas Possíveis
1. Cache do navegador desatualizado
2. Arquivos CSS não publicados no Azure
3. Problema de caminho dos arquivos estáticos

## ✅ Soluções

### Para o Usuário (Imediato)

1. **Hard Refresh:**
   - Pressione `Ctrl + Shift + R` (Windows/Linux)
   - Ou `Cmd + Shift + R` (Mac)
   - Ou `Ctrl + F5`

2. **Limpar Cache:**
   - Chrome/Edge: `Ctrl + Shift + Delete`
   - Marque "Imagens e arquivos em cache"
   - Clique em "Limpar dados"

3. **Modo Anônimo (Teste):**
   - `Ctrl + Shift + N` (Chrome/Edge)
   - Se funcionar no modo anônimo, é cache

### Para o Servidor (Definitivo)

1. **Republicar no Azure:**
```bash
# No Visual Studio
1. Botão direito no projeto
2. Publish
3. Publish novamente
```

2. **Verificar Arquivos Publicados:**
Confirme que estes arquivos existem no Azure:
- `/wwwroot/css/custom-styles.css`
- `/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css`
- `/wwwroot/lib/bootstrap-icons/font/bootstrap-icons.css`

3. **Reiniciar App Service:**
```bash
# No Portal Azure
1. Vá para o App Service
2. Clique em "Restart"
3. Aguarde 1-2 minutos
```

4. **Forçar Atualização de Cache:**
Adicione versão nos arquivos CSS (já está no _Layout.cshtml):
```html
<link href="/css/custom-styles.css?v=2" rel="stylesheet">
```

## 🔧 Solução Permanente

Edite `WebAppSystems/Views/Login/Index.cshtml` e adicione versão:

```html
<link href="/lib/bootstrap/dist/css/bootstrap.min.css?v=2" rel="stylesheet">
<link href="/lib/bootstrap-icons/font/bootstrap-icons.css?v=2" rel="stylesheet">
<link href="/css/custom-styles.css?v=2" rel="stylesheet">
```

Depois republique no Azure.

## 📊 Verificar se Funcionou

1. Abra o DevTools (F12)
2. Vá na aba "Network"
3. Recarregue a página
4. Verifique se os arquivos CSS carregam com status 200
5. Se aparecer 404, os arquivos não foram publicados

## 🚨 Se Nada Funcionar

Execute no servidor:
```bash
# Limpar e republicar
dotnet clean
dotnet publish -c Release
```

Depois faça deploy manual dos arquivos da pasta `bin/Release/net6.0/publish/`
