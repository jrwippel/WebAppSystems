# Como Limpar o Cache do Favicon

O favicon antigo ainda aparece porque está em cache no navegador. Siga estes passos:

## Método 1: Hard Refresh (Mais Rápido)
1. Abra o sistema no navegador
2. Pressione **Ctrl + Shift + R** (Windows/Linux) ou **Cmd + Shift + R** (Mac)
3. Ou pressione **Ctrl + F5**

## Método 2: Limpar Cache do Navegador

### Chrome/Edge:
1. Pressione **Ctrl + Shift + Delete**
2. Selecione "Imagens e arquivos em cache"
3. Escolha "Todo o período"
4. Clique em "Limpar dados"

### Firefox:
1. Pressione **Ctrl + Shift + Delete**
2. Marque "Cache"
3. Clique em "Limpar agora"

## Método 3: Modo Anônimo (Para Testar)
1. Abra uma janela anônima/privada
2. Acesse o sistema
3. O novo favicon deve aparecer

## Método 4: Forçar Atualização do Favicon
1. Acesse diretamente: `http://seu-site.com/favicon.svg?v=2`
2. Depois volte para a página principal
3. Pressione F5

## Verificar se Funcionou
- Olhe a aba do navegador
- O ícone deve ser um cronômetro roxo/azul
- Se ainda aparecer o antigo, tente o Método 2

## Para Produção
Após fazer deploy:
1. Reinicie a aplicação no servidor
2. Todos os usuários precisarão fazer Hard Refresh (Ctrl + F5)
3. Ou aguardar o cache expirar naturalmente (pode levar horas/dias)

## Nota Técnica
Adicionei `?v=2` nas URLs do favicon para forçar o navegador a baixar a nova versão.
Sempre que mudar o favicon no futuro, incremente este número (v=3, v=4, etc).
