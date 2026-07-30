# 🗺️ Roadmap - Pendências e Próximos Passos

**Atualizado em:** 30/07/2026

---

## ✅ Já Feito e Liberado em PROD

- [x] Hierarquia e Subordinação (gestor + alertas 48h)
- [x] Prevenção de duplicados (CNPJ obrigatório + validação)
- [x] Padronização de texto (Title Case automático)
- [x] Privacidade (removidos data nascimento e telefone)
- [x] Preferências (removido "utiliza bordas")
- [x] Treinamento/Onboarding (Central de Ajuda - opção criada dentro do sistema para os usuários)
- [x] Notificações de lançamento (email automático)
- [x] Limpeza de usuários teste — excluir "teste financeiro" e "padrão"

---

## 📋 Próximos Dias — Esforço Baixo (~1h cada)

| # | Item | Status | Observação |
|---|------|--------|------------|
| 1 | Sessão 4h — aumentar timeout de inatividade | ✅ Feito | Configurado 240 min no Program.cs |
| 2 | Limpeza de usuários teste — excluir "teste financeiro" e "padrão" | ⏳ Pendente | |
| 3 | Listagem clientes em ordem alfabética | ⚠️ Verificar | Já está, mas verificar todas as telas |
| 4 | Reestruturação de Áreas — migrar lançamentos de Penal para Tributário e excluir | ⏳ Pendente | Pode ser simplesmente alterada a área de todos os lançamentos da Penal para Tributário |
| 5 | Lançamentos cruzados | ✅ Já funciona | Sistema aceita qualquer área no lançamento independente da área do usuário (sem alteração necessária) |

---

## 🔧 Médio Esforço (1-3 horas cada)

| # | Item | Status | Observação |
|---|------|--------|------------|
| 1 | Listagem clientes em ordem alfabética — verificar TODAS as telas | ⏳ Pendente | Garantir consistência em todo o sistema |
| 2 | Reestruturação de Áreas — migrar Penal → Tributário e excluir área | ⏳ Pendente | Alterar área de todos os lançamentos de Penal para Tributário, depois excluir a área Penal |

---

## 🏋️ Maior Esforço (~1 dia cada)

| # | Item | Status | Observação |
|---|------|--------|------------|
| 1 | Dashboards de rentabilidade (EHR, Taxa Realização, Margem) | ⏳ Pendente | |
| 2 | API REST para integração Office (endpoints GET/POST) | ⏳ Pendente | Verificar se existe custo para aquisição de algum plugin |
| 3 | Lançamentos de estagiários (fluxo específico) | ⏳ Pendente | Depende da regra — precisa de mais detalhes do que se espera |

---

## 📝 Notas

- **Lançamentos cruzados:** Confirmado que já funciona. Não precisa de alteração.
- **Reestruturação de Áreas:** A abordagem mais simples é um UPDATE no banco migrando todos os lançamentos da área "Penal" para "Tributário", e depois deletar a área "Penal".
- **API REST Office:** Antes de implementar, verificar custos de plugins/licenças necessárias.
- **Lançamentos estagiários:** Aguardando definição de regras de negócio.
