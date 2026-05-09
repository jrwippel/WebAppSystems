# Documento de Requisitos: Fluxo de Aprovação de Faturamento

## Introdução

Este documento especifica os requisitos para implementação de um fluxo de aprovação de faturamento no sistema de gestão para escritórios de advocacia. O fluxo permite que a equipe de Controladoria solicite aprovação de lançamentos de horas antes de marcá-los como faturados, garantindo revisão e validação pelos sócios responsáveis.

## Glossary

- **Sistema_Aprovacao**: Módulo responsável pelo fluxo de aprovação de faturamento
- **Lote_Aprovacao**: Entidade que agrupa lançamentos de horas de um cliente específico submetidos para aprovação
- **Lancamento**: Registro de horas trabalhadas (ProcessRecord) que pode ser incluído em um lote de aprovação
- **Usuario_Financeiro**: Usuário com perfil Admin e flag IsFinanceiro=true que pode criar lotes de aprovação (mutuamente exclusivo com IsAprovador)
- **Usuario_Aprovador**: Usuário com perfil Admin e flag IsAprovador=true que pode aprovar ou rejeitar lotes (mutuamente exclusivo com IsFinanceiro)
- **Notificador**: Componente responsável por registrar e exibir notificações in-app aos usuários via modal/popup (similar ao sistema de alerta de lançamento de horas existente) - NÃO envia emails
- **Status_Lote**: Estado atual de um lote (Pendente, Aprovado, Rejeitado, Cancelado, Faturado)
- **Historico_Aprovacao**: Registro de todas as ações realizadas em um lote
- **Abonar_Lancamento**: Ação de remover um lançamento do lote sem aprová-lo
- **Indicador_Totalizador**: Componente visual que soma horas e valores dos lançamentos aprovados em tempo real

## Requisitos

### Requisito 1: Configuração de Flags de Usuário

**User Story:** Como administrador do sistema, eu quero configurar flags IsFinanceiro e IsAprovador nos usuários, para controlar quem pode criar e aprovar lotes de faturamento.

#### Acceptance Criteria

1. THE Sistema_Aprovacao SHALL adicionar campos IsFinanceiro (boolean) e IsAprovador (boolean) no cadastro de usuários (Attorney)
2. WHEN um usuário com perfil Admin acessa o cadastro de usuário, THE Sistema_Aprovacao SHALL exibir radio buttons (mutuamente exclusivos) para selecionar: Nenhum, Financeiro ou Aprovador
3. IF o usuário logado não possui perfil Admin, THEN THE Sistema_Aprovacao SHALL ocultar os campos IsFinanceiro e IsAprovador
4. THE Sistema_Aprovacao SHALL garantir que as flags são mutuamente exclusivas através de radio buttons (apenas uma opção pode ser selecionada por vez)
5. WHEN o usuário seleciona "Financeiro", THE Sistema_Aprovacao SHALL definir IsFinanceiro=true e IsAprovador=false
6. WHEN o usuário seleciona "Aprovador", THE Sistema_Aprovacao SHALL definir IsAprovador=true e IsFinanceiro=false
7. WHEN o usuário seleciona "Nenhum", THE Sistema_Aprovacao SHALL definir IsFinanceiro=false e IsAprovador=false
8. WHEN as flags são alteradas, THE Sistema_Aprovacao SHALL registrar data da alteração e usuário que realizou a mudança
9. THE Sistema_Aprovacao SHALL validar que apenas usuários com perfil Admin podem ter flags IsFinanceiro ou IsAprovador ativas

### Requisito 2: Criação de Lote de Aprovação por Usuário Financeiro

**User Story:** Como usuário financeiro, eu quero criar lotes de aprovação selecionando clientes e período, para que os lançamentos sejam revisados antes do faturamento.

#### Acceptance Criteria

1. WHEN um usuário com perfil Admin e IsFinanceiro=true acessa o sistema, THE Sistema_Aprovacao SHALL exibir menu "Criar Lote de Aprovação"
2. WHEN o usuário acessa a tela de criação, THE Sistema_Aprovacao SHALL exibir interface para seleção de período (data início e fim)
3. WHEN o usuário seleciona um período, THE Sistema_Aprovacao SHALL listar todos os clientes que possuem Lancamentos não faturados naquele período
4. WHEN o usuário seleciona um ou mais clientes, THE Sistema_Aprovacao SHALL exibir todos os Lancamentos não faturados e não incluídos em lotes pendentes
5. WHEN o usuário confirma a criação, THE Sistema_Aprovacao SHALL criar um Lote_Aprovacao separado para cada cliente selecionado
6. WHEN um Lote_Aprovacao é criado, THE Sistema_Aprovacao SHALL registrar: data criação, Usuario_Financeiro criador, cliente, período, total de horas e valor estimado
7. IF um Lancamento já está incluído em um Lote_Aprovacao com status Pendente ou Aprovado, THEN THE Sistema_Aprovacao SHALL impedir sua inclusão em novo lote

### Requisito 3: Notificação de Lote Pendente para Aprovador

**User Story:** Como usuário aprovador, eu quero receber notificação quando houver lotes pendentes de aprovação, para que eu possa revisar em tempo hábil.

#### Acceptance Criteria

1. WHEN um Lote_Aprovacao é criado, THE Sistema_Aprovacao SHALL registrar notificação in-app para todos os usuários com IsAprovador=true
2. WHEN um Usuario_Aprovador faz login no sistema, THE Sistema_Aprovacao SHALL exibir modal/popup de notificação (similar ao alerta de lançamento de horas) informando sobre lotes pendentes
3. THE Sistema_Aprovacao SHALL utilizar o mesmo mecanismo de notificação in-app existente no sistema (modal/popup exibido após login)
4. THE Sistema_Aprovacao SHALL exibir na notificação: cliente, período, quantidade de lançamentos, total de horas e valor estimado
5. THE Sistema_Aprovacao SHALL incluir botão "Revisar Agora" na notificação que redireciona para a tela de revisão
6. WHEN um Usuario_Aprovador acessa o sistema, THE Sistema_Aprovacao SHALL exibir badge com contador de lotes pendentes no menu
7. THE Sistema_Aprovacao SHALL exibir lista de lotes pendentes na página inicial do Usuario_Aprovador ordenados por data de criação
8. THE Sistema_Aprovacao SHALL destacar visualmente lotes com mais de 3 dias pendentes
9. THE Sistema_Aprovacao SHALL permitir que Usuario_Aprovador marque notificação como lida ou adie para depois
10. THE Sistema_Aprovacao SHALL NOT enviar notificações por email - apenas notificações in-app via modal/popup

### Requisito 4: Revisão de Lote pelo Aprovador

**User Story:** Como usuário aprovador, eu quero revisar os lançamentos de um lote e ter opções de abonar ou editar, para garantir que apenas horas válidas sejam faturadas.

#### Acceptance Criteria

1. WHEN o Usuario_Aprovador seleciona um Lote_Aprovacao pendente, THE Sistema_Aprovacao SHALL exibir todos os Lancamentos incluídos com detalhes completos (data, advogado, descrição, horas, valor)
2. THE Sistema_Aprovacao SHALL exibir Indicador_Totalizador no topo da tela mostrando: total de lançamentos, horas totais e valor total estimado
3. WHEN o Usuario_Aprovador seleciona um Lancamento, THE Sistema_Aprovacao SHALL exibir opções: Aprovar, Abonar e Editar
4. WHEN o Usuario_Aprovador clica em "Abonar", THE Sistema_Aprovacao SHALL remover o Lancamento do lote e atualizar o Indicador_Totalizador
5. WHEN o Usuario_Aprovador clica em "Editar", THE Sistema_Aprovacao SHALL abrir modal permitindo alterar: descrição, horas e tipo de lançamento
6. WHEN um Lancamento é editado, THE Sistema_Aprovacao SHALL registrar no Historico_Aprovacao: campos alterados, valores anteriores e novos
7. WHEN o Usuario_Aprovador marca um Lancamento como aprovado, THE Sistema_Aprovacao SHALL atualizar o Indicador_Totalizador em tempo real
8. THE Sistema_Aprovacao SHALL permitir marcar/desmarcar múltiplos Lancamentos simultaneamente usando checkboxes

### Requisito 5: Indicador Totalizador em Tempo Real

**User Story:** Como usuário aprovador, eu quero ver um indicador que soma horas e valores à medida que aprovo lançamentos, para ter controle visual do progresso.

#### Acceptance Criteria

1. THE Sistema_Aprovacao SHALL exibir Indicador_Totalizador fixo no topo da tela de revisão
2. THE Indicador_Totalizador SHALL mostrar: quantidade de lançamentos aprovados/total, horas aprovadas/total e valor aprovado/total
3. WHEN o Usuario_Aprovador marca um Lancamento como aprovado, THE Sistema_Aprovacao SHALL atualizar o Indicador_Totalizador instantaneamente sem recarregar a página
4. WHEN o Usuario_Aprovador abona um Lancamento, THE Sistema_Aprovacao SHALL subtrair do total e atualizar o Indicador_Totalizador
5. WHEN o Usuario_Aprovador edita horas de um Lancamento, THE Sistema_Aprovacao SHALL recalcular e atualizar o Indicador_Totalizador
6. THE Sistema_Aprovacao SHALL exibir barra de progresso visual indicando percentual de lançamentos revisados

### Requisito 6: Aprovação Final do Lote

**User Story:** Como usuário aprovador, eu quero aprovar o lote completo após revisar todos os lançamentos, para liberar o faturamento.

#### Acceptance Criteria

1. WHEN o Usuario_Aprovador termina de revisar os Lancamentos, THE Sistema_Aprovacao SHALL exibir botão "Aprovar Lote"
2. WHEN o Usuario_Aprovador clica em "Aprovar Lote", THE Sistema_Aprovacao SHALL validar que todos os Lancamentos foram revisados (aprovados ou abonados)
3. IF existem Lancamentos não revisados, THEN THE Sistema_Aprovacao SHALL exibir mensagem de erro e destacar lançamentos pendentes
4. WHEN o lote é aprovado, THE Sistema_Aprovacao SHALL alterar Status_Lote para Aprovado e registrar data/hora e Usuario_Aprovador
5. WHEN o lote é aprovado, THE Sistema_Aprovacao SHALL marcar todos os Lancamentos aprovados com flag "AprovadoParaFaturamento=true"
6. WHEN o lote é aprovado, THE Sistema_Aprovacao SHALL liberar Lancamentos abonados para inclusão em novos lotes
7. THE Sistema_Aprovacao SHALL permitir que Usuario_Aprovador adicione comentário opcional ao aprovar o lote

### Requisito 7: Notificação de Aprovação para Usuário Financeiro

**User Story:** Como usuário financeiro, eu quero receber notificação quando um lote for aprovado, para que eu possa gerar e enviar a fatura ao cliente.

#### Acceptance Criteria

1. WHEN um Lote_Aprovacao é aprovado, THE Sistema_Aprovacao SHALL registrar notificação in-app para o Usuario_Financeiro que criou o lote
2. WHEN o Usuario_Financeiro faz login no sistema, THE Sistema_Aprovacao SHALL exibir modal/popup de notificação (similar ao alerta de lançamento de horas) informando sobre lotes aprovados
3. THE Sistema_Aprovacao SHALL utilizar o mesmo mecanismo de notificação in-app existente no sistema (modal/popup exibido após login)
4. THE Sistema_Aprovacao SHALL exibir na notificação: cliente, período, total de horas aprovadas e valor total
5. THE Sistema_Aprovacao SHALL incluir botão "Gerar Fatura" na notificação que redireciona para a tela de faturamento
6. WHEN o Usuario_Financeiro acessa o sistema, THE Sistema_Aprovacao SHALL exibir badge com contador de lotes aprovados pendentes de faturamento
7. THE Sistema_Aprovacao SHALL exibir lista de lotes aprovados na tela do Usuario_Financeiro com destaque visual
8. IF o Usuario_Aprovador adicionou comentário ao aprovar, THEN THE Sistema_Aprovacao SHALL exibir o comentário na notificação
9. THE Sistema_Aprovacao SHALL permitir que Usuario_Financeiro marque notificação como lida
10. THE Sistema_Aprovacao SHALL NOT enviar notificações por email - apenas notificações in-app via modal/popup

### Requisito 8: Geração e Envio de Fatura

**User Story:** Como usuário financeiro, eu quero gerar fatura de um lote aprovado e enviar por email ao cliente, para completar o ciclo de faturamento.

#### Acceptance Criteria

1. WHEN o Usuario_Financeiro acessa um Lote_Aprovacao com status Aprovado, THE Sistema_Aprovacao SHALL exibir botões "Gerar Fatura" e "Enviar Email"
2. WHEN o Usuario_Financeiro clica em "Gerar Fatura", THE Sistema_Aprovacao SHALL gerar PDF com todos os Lancamentos aprovados do lote
3. THE Sistema_Aprovacao SHALL incluir na fatura: dados do cliente, período, detalhamento de lançamentos, total de horas e valor total
4. WHEN o Usuario_Financeiro clica em "Enviar Email", THE Sistema_Aprovacao SHALL abrir modal para confirmar email do cliente e adicionar mensagem opcional
5. WHEN o email é enviado, THE Notificador SHALL anexar o PDF da fatura e enviar ao cliente
6. WHEN a fatura é enviada, THE Sistema_Aprovacao SHALL marcar todos os Lancamentos do lote como IsFaturado=true
7. WHEN a fatura é enviada, THE Sistema_Aprovacao SHALL alterar Status_Lote para Faturado e registrar data/hora
8. THE Sistema_Aprovacao SHALL permitir reenviar a fatura múltiplas vezes se necessário

### Requisito 9: Fluxo Contínuo de Aprovação

**User Story:** Como usuário aprovador, eu quero que o sistema me direcione automaticamente para o próximo lote pendente após aprovar um, para agilizar o processo de revisão.

#### Acceptance Criteria

1. WHEN o Usuario_Aprovador aprova um Lote_Aprovacao, THE Sistema_Aprovacao SHALL exibir mensagem de sucesso com opções: "Próximo Lote" ou "Voltar para Lista"
2. WHEN o Usuario_Aprovador clica em "Próximo Lote", THE Sistema_Aprovacao SHALL carregar automaticamente o próximo lote pendente mais antigo
3. IF não existem mais lotes pendentes, THEN THE Sistema_Aprovacao SHALL exibir mensagem "Todos os lotes foram revisados" e redirecionar para dashboard
4. THE Sistema_Aprovacao SHALL exibir indicador de progresso mostrando "Lote X de Y" durante a revisão
5. THE Sistema_Aprovacao SHALL permitir que Usuario_Aprovador pause a revisão e retome posteriormente do ponto onde parou

### Requisito 15: Controle de Permissões por Flags

**User Story:** Como administrador, eu quero que o sistema controle acesso às funcionalidades baseado nas flags IsFinanceiro e IsAprovador, para garantir segurança e segregação de funções.

#### Acceptance Criteria

1. THE Sistema_Aprovacao SHALL permitir acesso à tela "Criar Lote de Aprovação" apenas para usuários com perfil Admin E IsFinanceiro=true
2. THE Sistema_Aprovacao SHALL permitir acesso à tela "Revisar Lotes" apenas para usuários com perfil Admin E IsAprovador=true
3. THE Sistema_Aprovacao SHALL permitir acesso à tela "Gerar Fatura" apenas para usuários com perfil Admin E IsFinanceiro=true
4. IF um usuário sem as flags necessárias tenta acessar funcionalidade restrita, THEN THE Sistema_Aprovacao SHALL exibir mensagem "Acesso negado: você não possui permissão para esta funcionalidade"
5. THE Sistema_Aprovacao SHALL ocultar menus de funcionalidades para usuários sem as flags apropriadas
6. THE Sistema_Aprovacao SHALL registrar no log todas as tentativas de acesso negado para auditoria

### Requisito 12: Compatibilidade com Fluxo Atual de Faturamento

**User Story:** Como usuário do sistema, eu quero que o novo fluxo de aprovação seja opcional e não interfira no processo atual de lançamento e faturamento, para que o sistema continue funcionando normalmente.

#### Acceptance Criteria

1. THE Sistema_Aprovacao SHALL funcionar como módulo independente e opcional, não alterando o fluxo atual de lançamento de horas (ProcessRecord)
2. WHEN um Lancamento é criado, THE Sistema_Aprovacao SHALL permitir que ele seja marcado como faturado diretamente (fluxo atual) SE não estiver incluído em um Lote_Aprovacao pendente ou aprovado
3. THE Sistema_Aprovacao SHALL manter todas as funcionalidades existentes do módulo de Faturamento (/Faturamento) intactas
4. WHEN um usuário sem flags IsFinanceiro ou IsAprovador acessa o sistema, THE Sistema_Aprovacao SHALL ocultar completamente os menus e funcionalidades de aprovação
5. THE Sistema_Aprovacao SHALL permitir que Admin continue marcando lançamentos como faturados diretamente através da tela de Faturamento atual
6. IF um escritório não utiliza o fluxo de aprovação (nenhum usuário com flags), THEN THE Sistema_Aprovacao SHALL não interferir em nenhuma operação existente
7. THE Sistema_Aprovacao SHALL adicionar apenas novos campos opcionais nas tabelas existentes sem alterar campos atuais
8. WHEN um Lancamento é incluído em um Lote_Aprovacao, THE Sistema_Aprovacao SHALL apenas adicionar flag "EmAprovacao=true" sem alterar outros campos
9. THE Sistema_Aprovacao SHALL permitir que Admin remova um Lancamento de um lote pendente para permitir faturamento direto em casos excepcionais
10. THE Sistema_Aprovacao SHALL manter retrocompatibilidade total com lançamentos e faturamentos já existentes no banco de dados

### Requisito 13: Histórico e Auditoria de Aprovações

**User Story:** Como administrador, eu quero visualizar histórico completo de todas as ações realizadas em um lote, para fins de auditoria e rastreabilidade.

#### Acceptance Criteria

1. THE Sistema_Aprovacao SHALL registrar no Historico_Aprovacao todas as ações realizadas em um Lote_Aprovacao: criação, edição de lançamentos, abonos, aprovação e faturamento
2. THE Sistema_Aprovacao SHALL registrar para cada ação: data/hora, usuário, tipo de ação, detalhes (campos alterados, valores anteriores/novos)
3. WHEN um usuário com perfil Admin acessa um Lote_Aprovacao, THE Sistema_Aprovacao SHALL exibir aba "Histórico" com todas as ações ordenadas por data decrescente
4. THE Sistema_Aprovacao SHALL preservar histórico indefinidamente mesmo após faturamento
5. THE Sistema_Aprovacao SHALL permitir exportar histórico em formato PDF para auditoria externa

### Requisito 11: Relatórios de Aprovação

**User Story:** Como administrador, eu quero gerar relatórios de solicitações de aprovação, para análise de desempenho e controle gerencial.

#### Acceptance Criteria

1. THE Sistema_Aprovacao SHALL permitir gerar relatório de solicitações por período, status, cliente ou aprovador
2. THE Sistema_Aprovacao SHALL incluir no relatório: número da solicitação, data criação, cliente, criador, aprovador, status, total horas, valor estimado
3. THE Sistema_Aprovacao SHALL calcular métricas: tempo médio de aprovação, taxa de aprovação, taxa de rejeição
4. THE Sistema_Aprovacao SHALL permitir exportar relatório em formato Excel e PDF
5. WHEN o relatório é gerado, THE Sistema_Aprovacao SHALL ordenar solicitações por data de criação decrescente

### Requisito 12: Dashboard de Aprovações

**User Story:** Como usuário do sistema, eu quero visualizar dashboard com indicadores de aprovações, para acompanhar status e pendências rapidamente.

#### Acceptance Criteria

1. THE Sistema_Aprovacao SHALL exibir para Controladoria: total de solicitações pendentes, aprovadas aguardando faturamento e rejeitadas
2. THE Sistema_Aprovacao SHALL exibir para Aprovadores: total de solicitações pendentes de sua aprovação e tempo médio de resposta
3. THE Sistema_Aprovacao SHALL exibir gráfico de evolução de solicitações por mês (criadas, aprovadas, rejeitadas, faturadas)
4. THE Sistema_Aprovacao SHALL exibir lista das 5 solicitações mais antigas pendentes de aprovação
5. WHEN o usuário clica em um indicador, THE Sistema_Aprovacao SHALL navegar para lista filtrada correspondente

### Requisito 13: Aprovação Parcial de Lançamentos

**User Story:** Como sócio responsável, eu quero aprovar apenas alguns lançamentos de uma solicitação, para que eu possa rejeitar itens específicos sem rejeitar toda a solicitação.

#### Acceptance Criteria

1. WHEN o Aprovador revisa uma Solicitacao_Aprovacao, THE Sistema_Aprovacao SHALL permitir selecionar individualmente quais Lancamentos aprovar
2. WHEN o Aprovador aprova parcialmente, THE Sistema_Aprovacao SHALL criar nova Solicitacao_Aprovacao com status Aprovada contendo apenas Lancamentos aprovados
3. WHEN o Aprovador aprova parcialmente, THE Sistema_Aprovacao SHALL alterar status da solicitação original para Rejeitada e registrar motivo automático
4. WHEN o Aprovador aprova parcialmente, THE Sistema_Aprovacao SHALL liberar Lancamentos não aprovados para nova solicitação
5. THE Sistema_Aprovacao SHALL registrar no Historico_Aprovacao a relação entre solicitação original e nova solicitação parcial

### Requisito 14: Prazo de Aprovação

**User Story:** Como administrador, eu quero configurar prazo para aprovação de solicitações, para que aprovadores sejam alertados sobre solicitações atrasadas.

#### Acceptance Criteria

1. THE Sistema_Aprovacao SHALL permitir configurar prazo padrão de aprovação em dias úteis
2. WHEN uma Solicitacao_Aprovacao ultrapassa o prazo configurado sem aprovação, THE Sistema_Aprovacao SHALL marcar como atrasada
3. WHEN uma solicitação está atrasada, THE Sistema_Aprovacao SHALL exibir notificação in-app via modal/popup ao Aprovador no próximo login
4. THE Sistema_Aprovacao SHALL exibir indicador visual de atraso nas solicitações pendentes
5. THE Sistema_Aprovacao SHALL incluir solicitações atrasadas no dashboard com destaque

### Requisito 15: Comentários e Observações

**User Story:** Como usuário do sistema, eu quero adicionar comentários em uma solicitação, para comunicação entre Controladoria e Aprovador.

#### Acceptance Criteria

1. THE Sistema_Aprovacao SHALL permitir que criador e Aprovador adicionem comentários em uma Solicitacao_Aprovacao
2. WHEN um comentário é adicionado, THE Sistema_Aprovacao SHALL registrar data/hora, usuário e texto do comentário
3. WHEN um comentário é adicionado, THE Sistema_Aprovacao SHALL exibir notificação in-app via modal/popup ao outro participante (criador ou aprovador) no próximo login
4. THE Sistema_Aprovacao SHALL exibir todos os comentários ordenados por data crescente
5. THE Sistema_Aprovacao SHALL permitir comentários em solicitações com qualquer status exceto Cancelada
