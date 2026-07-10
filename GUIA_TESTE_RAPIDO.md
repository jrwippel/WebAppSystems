# Guia de Teste Rápido - Fluxo de Aprovação

## 🔧 Passo 1: Atualizar Flags no Banco de Dados

Execute o script SQL para garantir que os usuários têm as permissões corretas:

```bash
# Conectar ao banco de dados MySQL
mysql -u root -p WebAppSystems

# Ou se estiver usando outro usuário/banco:
mysql -u [seu_usuario] -p [nome_do_banco]
```

Depois execute os comandos:

```sql
-- Verificar flags atuais
SELECT Id, Name, Login, Perfil, IsFinanceiro, IsAprovador, Inativo
FROM Attorney
WHERE Id IN (1, 2, 6)
ORDER BY Id;

-- Atualizar flags
UPDATE Attorney SET IsFinanceiro = 1, IsAprovador = 0 WHERE Id = 1;
UPDATE Attorney SET IsFinanceiro = 1, IsAprovador = 0 WHERE Id = 2;
UPDATE Attorney SET IsFinanceiro = 0, IsAprovador = 1 WHERE Id = 6;

-- Verificar novamente
SELECT Id, Name, Login, Perfil, IsFinanceiro, IsAprovador, Inativo
FROM Attorney
WHERE Id IN (1, 2, 6)
ORDER BY Id;
```

**Resultado Esperado:**
```
Id | Name            | Login           | Perfil | IsFinanceiro | IsAprovador | Inativo
1  | Fianceiro       | financeiro      | Admin  | 1            | 0           | 0
2  | Jackson Wippel  | jackson.wippel  | Admin  | 1            | 0           | 0
6  | Aprovador       | aprovador       | Admin  | 0            | 1           | 0
```

---

## 🚀 Passo 2: Iniciar a Aplicação

```bash
cd WebAppSystems
dotnet run
```

Aguarde a mensagem:
```
Now listening on: https://localhost:5095
Now listening on: http://localhost:8000
```

---

## 🧪 Passo 3: Testar como Usuário Financeiro

### 3.1. Login
1. Acesse: https://localhost:5095
2. Faça login com:
   - **Login:** `financeiro`
   - **Senha:** [senha do usuário]

### 3.2. Verificar Menu
✅ Deve aparecer seção "Aprovação" com:
- 📋 Meus Lotes
- ➕ Criar Lote

### 3.3. Testar "Meus Lotes"
1. Clique em "Meus Lotes"
2. Deve listar os lotes criados por você
3. **TESTE CRÍTICO:** Clique em "Detalhes" de um lote
   - ✅ Deve abrir a tela de detalhes
   - ❌ NÃO deve redirecionar para Home

### 3.4. Testar "Criar Lote"
1. Clique em "Criar Lote"
2. Selecione período (ex: 01/04/2026 a 30/04/2026)
3. Clique em "Buscar Lançamentos"
4. **TESTE CRÍTICO:** Campo de clientes
   - ✅ Deve ser um Select2 (dropdown com busca)
   - ✅ Deve permitir digitar para filtrar
   - ✅ Deve permitir selecionar múltiplos clientes
5. Selecione um ou mais clientes
6. Clique em "Criar Lote"

---

## 🧪 Passo 4: Testar como Usuário Aprovador

### 4.1. Logout e Login
1. Faça logout
2. Faça login com:
   - **Login:** `aprovador`
   - **Senha:** [senha do usuário]

### 4.2. Verificar Menu
✅ Deve aparecer seção "Aprovação" com:
- 📝 Revisar Lotes
- 🔔 Notificações

### 4.3. Testar "Revisar Lotes"
1. Clique em "Revisar Lotes"
2. Deve listar lotes pendentes
3. Clique em um lote para revisar

### 4.4. Testar Tela de Revisão (CRÍTICO)

**Abra o Console do Navegador (F12 → Console)**

Deve aparecer:
```
[DEBUG] Script Revisar.cshtml carregado
[DEBUG] Bootstrap disponível: true
[DEBUG] jQuery disponível: true
[DEBUG] JSON ValoresCliente: [...]
[DEBUG] Valores cliente parseados: [...]
```

❌ **NÃO deve aparecer:**
```
ReferenceError: aprovarLote is not defined
```

**Teste os Botões:**

1. **Botão Editar (ícone lápis):**
   - ✅ Clique no botão editar de um lançamento
   - ✅ Campos devem ficar editáveis (fundo amarelo)
   - ✅ Botões Salvar e Cancelar devem aparecer

2. **Botão Salvar (ícone check):**
   - ✅ Edite a descrição ou horas
   - ✅ Clique em Salvar
   - ✅ Deve salvar e mostrar badge "Editado"

3. **Botão Cancelar (ícone X):**
   - ✅ Clique em Editar
   - ✅ Faça alterações
   - ✅ Clique em Cancelar
   - ✅ Deve restaurar valores originais

4. **Botão Aprovar (ícone check-circle verde):**
   - ✅ Clique em Aprovar
   - ✅ Badge deve mudar para "Aprovado" (verde)
   - ✅ Totalizador deve atualizar:
     - "Horas Aprovadas" deve aumentar
     - "Revisados" deve aumentar
     - Barra de progresso deve avançar

5. **Botão Abonar (ícone x-circle vermelho):**
   - ✅ Clique em Abonar
   - ✅ Modal deve abrir pedindo observação
   - ✅ Clique em "Abonar"
   - ✅ Badge deve mudar para "Abonado" (vermelho)
   - ✅ Linha deve ficar com opacidade reduzida
   - ✅ Totalizador deve atualizar:
     - "Horas Abonadas" deve aumentar
     - "Total do Lote" deve diminuir
     - "Revisados" deve aumentar
     - Barra de progresso deve avançar

6. **Botão Aprovar Lote (topo da tela):**
   - ✅ Revise todos os lançamentos (aprovar ou abonar)
   - ✅ Clique em "Aprovar Lote"
   - ✅ Modal deve abrir
   - ✅ Clique em "Confirmar Aprovação"
   - ✅ Deve aprovar o lote e redirecionar

---

## 📊 Verificar Totalizador em Tempo Real

O totalizador no topo deve atualizar automaticamente:

```
┌─────────────────────────────────────────────────────────────────┐
│ Total do Lote | Horas Aprovadas | Horas Abonadas | Revisados   │
│    10.00h     |     8.00h       |     2.00h      |   10 / 10   │
│ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ 100% revisado │
└─────────────────────────────────────────────────────────────────┘
```

**Regras:**
- ✅ **Total do Lote** = Total original - Horas Abonadas
- ✅ **Horas Aprovadas** = Soma APENAS dos itens aprovados (NÃO inclui abonados)
- ✅ **Horas Abonadas** = Soma dos itens abonados
- ✅ **Revisados** = Aprovados + Abonados
- ✅ **Progresso** = (Revisados / Total) × 100%

---

## ❌ Problemas Conhecidos e Soluções

### Problema: Botões não funcionam
**Sintoma:** Clicar nos botões não faz nada

**Solução:**
1. Abra o Console do Navegador (F12)
2. Verifique se há erros JavaScript
3. Se houver `ReferenceError`, o script não carregou corretamente
4. Faça Ctrl+F5 para limpar cache e recarregar

### Problema: Redirecionamento para Home
**Sintoma:** Ao clicar em "Detalhes", vai para Home com erro

**Solução:**
1. Verifique se executou o script SQL do Passo 1
2. Faça logout e login novamente
3. Verifique no banco se `IsFinanceiro=1` para o usuário

### Problema: Select2 não funciona
**Sintoma:** Campo de clientes não permite busca

**Solução:**
1. Verifique se jQuery e Select2 estão carregados
2. Abra Console (F12) e procure por erros
3. Faça Ctrl+F5 para limpar cache

---

## ✅ Checklist Final

- [ ] Script SQL executado
- [ ] Usuário Financeiro consegue acessar "Meus Lotes"
- [ ] Usuário Financeiro consegue ver "Detalhes" sem erro
- [ ] Usuário Financeiro consegue criar lote com Select2
- [ ] Usuário Aprovador consegue acessar "Revisar Lotes"
- [ ] Botão Editar funciona
- [ ] Botão Salvar funciona
- [ ] Botão Cancelar funciona
- [ ] Botão Aprovar funciona
- [ ] Botão Abonar funciona
- [ ] Totalizador atualiza em tempo real
- [ ] Botão Aprovar Lote funciona
- [ ] Console do navegador NÃO mostra erros JavaScript

---

## 📝 Reportar Problemas

Se encontrar algum problema, anote:
1. Qual usuário estava logado
2. Qual tela estava acessando
3. Qual botão clicou
4. Mensagem de erro (se houver)
5. Erros no Console do navegador (F12)
