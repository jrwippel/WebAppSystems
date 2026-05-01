# Sistema de Controle de Limite de Uso da IA

## Resumo
Implementado um sistema completo de controle de uso da IA com limite de 10 consultas por dia por usuário, incluindo mensagens de aviso e painel administrativo.

## Componentes Implementados

### 1. Modelo de Dados
- **AIUsageLimit.cs**: Modelo para rastrear uso diário por usuário
  - AttorneyId: ID do usuário
  - Date: Data do uso
  - UsageCount: Quantidade usada no dia
  - DailyLimit: Limite diário (padrão: 10)
  - CreatedAt/UpdatedAt: Timestamps

### 2. Service de Controle
- **AIUsageLimitService.cs**: Service principal para gerenciar limites
  - `CanUseAIAsync()`: Verifica se usuário pode usar IA
  - `RegisterAIUsageAsync()`: Registra uso da IA
  - `GetUsageStatsAsync()`: Obtém estatísticas de uso
  - `UpdateDailyLimitAsync()`: Atualiza limite (admin)
  - `CleanupOldRecordsAsync()`: Limpa registros antigos

### 3. Controllers Atualizados
- **PainelGestaoController.cs**: 
  - Verificação de limite antes de análise de gráficos
  - Endpoint para estatísticas de uso
  - Registro de uso após análise bem-sucedida

- **DocumentAnalysisController.cs**:
  - Verificação de limite antes de upload
  - Registro de uso após processamento
  - Endpoint para estatísticas de uso

- **AIUsageAdminController.cs**: Novo controller para administração
  - Visualização de uso por usuário
  - Atualização de limites
  - Reset de uso diário
  - Histórico de uso
  - Estatísticas gerais

### 4. Interface de Usuário

#### Indicador de Uso (ai-usage-indicator.js)
- Componente JavaScript reutilizável
- Mostra uso atual, limite e restante
- Barra de progresso visual
- Alertas baseados no status:
  - Verde: Normal (>2 usos restantes)
  - Amarelo: Próximo do limite (≤2 usos)
  - Vermelho: Limite atingido (0 usos)
- Atualização automática a cada 30 segundos
- Função global `canUseAI()` para verificar antes de usar

#### Views Atualizadas
- **DocumentAnalysis/Index.cshtml**: Indicador de uso + verificação antes de upload
- **PainelGestao/Index.cshtml**: Indicador de uso + verificação antes de análise

#### Painel Administrativo
- **AIUsageAdmin/Index.cshtml**: Interface completa para administradores
  - Estatísticas gerais (usuários ativos, uso hoje, últimos 7 dias, usuários no limite)
  - Tabela de usuários com status atual
  - Ações por usuário:
    - Atualizar limite diário
    - Reset de uso diário
    - Ver histórico de 30 dias
  - Interface responsiva com cores indicativas

### 5. Mensagens de Controle

#### Para Usuários
- **Limite atingido**: "Limite diário de 10 consultas de IA atingido. Aguarde até amanhã ou entre em contato com o administrador para upgrade do plano."
- **Próximo do limite**: "Atenção! Você tem apenas X consulta(s) restante(s) hoje."
- **Status normal**: Mostra quantas consultas restam

#### Para Administradores
- Controle total sobre limites individuais
- Possibilidade de reset emergencial
- Histórico detalhado de uso
- Estatísticas consolidadas

## Fluxo de Funcionamento

### 1. Verificação Antes do Uso
```javascript
// Antes de qualquer operação de IA
const canUse = await canUseAI();
if (!canUse) {
    return; // Bloqueia a operação
}
```

### 2. Registro Após o Uso
```csharp
// Após operação bem-sucedida
await _aiUsageLimitService.RegisterAIUsageAsync(usuario.Id);
```

### 3. Resposta com Limite
```csharp
// Controller retorna informações de limite
return Ok(new { 
    insight, 
    remainingUses = remainingUses - 1 
});
```

## Configuração e Instalação

### 1. Migration Aplicada
```bash
dotnet ef migrations add AddAIUsageLimit
dotnet ef database update
```

### 2. Service Registrado
```csharp
// Program.cs
builder.Services.AddScoped<AIUsageLimitService>();
```

### 3. Tabela Criada
- `AIUsageLimit` com índice em `AttorneyId`
- Foreign key para `Attorney`
- Campos para controle de uso e timestamps

## Funcionalidades Administrativas

### Painel de Controle (/AIUsageAdmin)
- **Acesso**: Apenas administradores
- **Funcionalidades**:
  - Visualizar uso de todos os usuários
  - Alterar limites individuais (0-1000)
  - Reset de uso diário para emergências
  - Histórico detalhado por usuário
  - Estatísticas consolidadas

### Limpeza Automática
- Registros mantidos por 30 dias
- Método `CleanupOldRecordsAsync()` disponível para job/cron

## Segurança e Validação

### Validações Implementadas
- Limite mínimo: 0 consultas
- Limite máximo: 1000 consultas
- Verificação de sessão válida
- Autorização por perfil (admin para configurações)

### Tratamento de Erros
- Logs detalhados de todas as operações
- Fallback gracioso em caso de erro
- Mensagens de erro amigáveis ao usuário

## Extensibilidade

### Limites Personalizados
- Fácil alteração do limite padrão (atualmente 10)
- Limites individuais por usuário
- Possibilidade de limites por perfil/plano

### Novos Recursos
- Base preparada para diferentes tipos de consulta
- Histórico mantido para análises futuras
- API pronta para integração com sistemas de cobrança

## Status: ✅ IMPLEMENTADO E FUNCIONAL

O sistema está completamente implementado e pronto para uso, incluindo:
- ✅ Controle de limite por usuário
- ✅ Interface visual de status
- ✅ Painel administrativo
- ✅ Mensagens de aviso
- ✅ Bloqueio automático
- ✅ Histórico e estatísticas
- ✅ Migration aplicada
- ✅ Services registrados