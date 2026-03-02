# 🌍 Guia de Implementação de Tradução

## 📝 Instruções Passo a Passo

### Passo 1: Adicionar Chaves de Tradução no _Layout.cshtml

Localize o objeto `translations` no arquivo `WebAppSystems/Views/Shared/_Layout.cshtml` (por volta da linha 156) e adicione as seguintes chaves:

```javascript
const translations = {
    pt: {
        // ... traduções existentes do menu, home, reports ...
        
        // ========== CHAVES COMUNS ==========
        'common.actions': 'Ações',
        'common.save': 'Salvar',
        'common.saveChanges': 'Salvar Alterações',
        'common.cancel': 'Cancelar',
        'common.delete': 'Excluir',
        'common.confirmDelete': 'Confirmar Exclusão',
        'common.edit': 'Editar',
        'common.details': 'Detalhes',
        'common.back': 'Voltar',
        'common.backToList': 'Voltar para Lista',
        'common.search': 'Buscar',
        'common.new': 'Novo',
        'common.showing': 'Mostrando',
        'common.of': 'de',
        'common.records': 'registros',
        'common.deleteWarning': 'Você tem certeza que deseja excluir?',
        'common.cannotUndo': 'Esta ação não pode ser desfeita',
        'common.allDataRemoved': 'Todos os dados associados serão permanentemente removidos do sistema',
        'common.completeInfo': 'Informações completas',
        'common.updateInfo': 'Atualize as informações',
        'common.name': 'Nome',
        'common.email': 'Email',
        'common.phone': 'Telefone',
        'common.document': 'Documento',
        'common.client': 'Cliente',
        'common.department': 'Área',
        'common.value': 'Valor',
        'common.percentage': 'Percentual',
        
        // ========== ATTORNEYS (USUÁRIOS) ==========
        'attorneys.index.title': 'Usuários',
        'attorneys.index.subtitle': 'Gerencie os usuários do sistema',
        'attorneys.index.newButton': 'Novo Usuário',
        'attorneys.index.searchPlaceholder': 'Buscar por nome, email ou login...',
        'attorneys.index.colName': 'Nome',
        'attorneys.index.colEmail': 'Email',
        'attorneys.index.colProfile': 'Perfil',
        'attorneys.index.colDepartment': 'Área',
        'attorneys.details.title': 'Detalhes do Usuário',
        'attorneys.details.subtitle': 'Informações completas',
        'attorneys.details.basicInfo': 'Informações Básicas',
        'attorneys.details.contactInfo': 'Informações de Contato',
        'attorneys.details.preferences': 'Preferências',
        'attorneys.details.birthDate': 'Data de Nascimento',
        'attorneys.details.login': 'Login',
        'attorneys.edit.title': 'Editar Usuário',
        'attorneys.edit.subtitle': 'Atualize as informações do usuário',
        'attorneys.edit.basicInfoSection': 'Informações Básicas',
        'attorneys.edit.contactSection': 'Contato',
        'attorneys.edit.accessSection': 'Acesso',
        'attorneys.edit.preferencesSection': 'Preferências',
        'attorneys.delete.title': 'Excluir Usuário',
        'attorneys.delete.warning': 'Você tem certeza que deseja excluir este usuário?',
        'attorneys.delete.allData': 'Todos os dados associados a este usuário serão permanentemente removidos do sistema',
        
        // ========== CLIENTS (CLIENTES) ==========
        'clients.index.title': 'Clientes',
        'clients.index.subtitle': 'Gerencie seus clientes',
        'clients.index.newButton': 'Novo Cliente',
        'clients.index.searchPlaceholder': 'Buscar por nome, email, documento ou telefone...',
        'clients.index.colName': 'Nome',
        'clients.index.colEmail': 'Email',
        'clients.index.colDocument': 'Documento',
        'clients.index.colPhone': 'Telefone',
        'clients.index.colRequester': 'Solicitante',
        'clients.index.colStatus': 'Status',
        'clients.index.statusActive': 'Ativo',
        'clients.index.statusInactive': 'Inativo',
        'clients.index.statusInternal': 'Interno',
        'clients.details.title': 'Detalhes do Cliente',
        'clients.details.subtitle': 'Informações completas',
        'clients.details.contactInfo': 'Informações de Contato',
        'clients.details.documentation': 'Documentação',
        'clients.details.requester': 'Solicitante',
        'clients.edit.title': 'Editar Cliente',
        'clients.edit.subtitle': 'Atualize as informações do cliente',
        'clients.edit.basicInfo': 'Informações Básicas',
        'clients.edit.contact': 'Contato',
        'clients.edit.additionalInfo': 'Informações Adicionais',
        'clients.edit.internalClient': 'Cliente Interno',
        'clients.edit.internalClientDesc': 'Marque se for cliente interno da empresa',
        'clients.edit.inactiveClient': 'Cliente Inativo',
        'clients.edit.inactiveClientDesc': 'Marque para desativar este cliente',
        'clients.edit.changeImage': 'Alterar Imagem',
        'clients.edit.imageHint': 'PNG, JPG ou GIF (máx. 5MB)',
        'clients.delete.title': 'Excluir Cliente',
        'clients.delete.warning': 'Você tem certeza que deseja excluir este cliente?',
        
        // ========== DEPARTMENTS (ÁREAS) ==========
        'departments.index.title': 'Áreas',
        'departments.index.subtitle': 'Gerencie as áreas da empresa',
        'departments.index.newButton': 'Nova Área',
        'departments.index.searchPlaceholder': 'Buscar área...',
        'departments.index.colName': 'Nome da Área',
        'departments.details.title': 'Detalhes da Área',
        'departments.details.subtitle': 'Informações completas',
        'departments.details.areaName': 'Nome da Área',
        'departments.details.id': 'ID',
        'departments.edit.title': 'Editar Área',
        'departments.edit.subtitle': 'Atualize as informações da área',
        'departments.edit.areaIcon': 'Ícone da Área',
        'departments.edit.areaName': 'Nome da Área',
        'departments.edit.placeholder': 'Digite o nome da área',
        'departments.delete.title': 'Excluir Área',
        'departments.delete.warning': 'Você tem certeza que deseja excluir esta área?',
        'departments.delete.allData': 'Todos os dados associados a esta área serão permanentemente removidos do sistema',
        'departments.delete.companyArea': 'Área da Empresa',
        
        // ========== MENSALISTAS ==========
        'mensalistas.index.title': 'Mensalistas',
        'mensalistas.index.subtitle': 'Gerencie contratos mensais',
        'mensalistas.index.newButton': 'Novo Mensalista',
        'mensalistas.index.searchPlaceholder': 'Buscar por cliente...',
        'mensalistas.index.colClient': 'Cliente',
        'mensalistas.index.colGrossMonthly': 'Valor Mensal Bruto',
        'mensalistas.index.colPartnerCommission': 'Comissão Parceiro',
        'mensalistas.index.colPartnerCommission': 'Comissão Sócio',
        'mensalistas.details.title': 'Detalhes do Mensalista',
        'mensalistas.details.subtitle': 'Informações do contrato mensal',
        'mensalistas.details.monthlyClient': 'Cliente Mensalista',
        'mensalistas.details.grossMonthly': 'Valor Mensal Bruto',
        'mensalistas.details.partnerCommission': 'Comissão Parceiro',
        'mensalistas.details.partnerCommission': 'Comissão Sócio',
        'mensalistas.edit.title': 'Editar Mensalista',
        'mensalistas.edit.subtitle': 'Atualize os valores do contrato',
        'mensalistas.edit.clientSection': 'Cliente',
        'mensalistas.edit.valuesSection': 'Valores',
        'mensalistas.delete.title': 'Excluir Mensalista',
        'mensalistas.delete.warning': 'Você tem certeza que deseja excluir este mensalista?',
        'mensalistas.delete.allData': 'Todos os dados do contrato mensal serão permanentemente removidos do sistema',
        
        // ========== VALOR CLIENTES ==========
        'valorclientes.index.title': 'Valores',
        'valorclientes.index.subtitle': 'Gerencie os valores por hora',
        'valorclientes.index.newButton': 'Novo Valor',
        'valorclientes.index.searchPlaceholder': 'Buscar por cliente ou advogado...',
        'valorclientes.index.colClient': 'Cliente',
        'valorclientes.index.colAttorney': 'Advogado/Sócio',
        'valorclientes.index.colValue': 'Valor/Hora',
        'valorclientes.details.title': 'Detalhes do Valor',
        'valorclientes.details.subtitle': 'Informações completas',
        'valorclientes.details.hourlyValue': 'Valor por hora',
        'valorclientes.edit.title': 'Editar Valor',
        'valorclientes.edit.subtitle': 'Atualize o valor por hora',
        'valorclientes.edit.selectClient': 'Selecione o cliente',
        'valorclientes.edit.selectAttorney': 'Selecione o advogado',
        'valorclientes.delete.title': 'Excluir Valor',
        'valorclientes.delete.warning': 'Você tem certeza que deseja excluir este valor?',
        'valorclientes.delete.allData': 'O registro de valor por hora será permanentemente removido do sistema',
        
        // ========== PERCENTUAL AREAS ==========
        'percentualareas.index.title': 'Percentuais por Cliente/Área',
        'percentualareas.index.subtitle': 'Gerencie os percentuais de distribuição',
        'percentualareas.index.newButton': 'Novo Registro',
        'percentualareas.index.searchPlaceholder': 'Buscar por cliente ou área...',
        'percentualareas.index.colClient': 'Cliente',
        'percentualareas.index.colArea': 'Área',
        'percentualareas.index.colPercentage': 'Percentual',
        'percentualareas.details.title': 'Detalhes do Percentual',
        'percentualareas.details.subtitle': 'Informações completas',
        'percentualareas.edit.title': 'Editar Percentual',
        'percentualareas.edit.subtitle': 'Atualize o percentual do cliente/área',
        'percentualareas.edit.selectClient': 'Selecione o cliente',
        'percentualareas.edit.selectArea': 'Selecione a área',
        'percentualareas.create.title': 'Novo Percentual',
        'percentualareas.create.subtitle': 'Cadastre um novo percentual por cliente/área',
        'percentualareas.delete.title': 'Excluir Percentual',
        'percentualareas.delete.warning': 'Você tem certeza que deseja excluir este percentual?',
        'percentualareas.delete.allData': 'O registro de percentual será permanentemente removido do sistema',
        
        // ========== MENSALISTA RESULTADOS ==========
        'mensalista.index.title': 'Resultado do Mês',
        'mensalista.index.subtitle': 'Consulte os resultados mensais',
        'mensalista.index.monthYear': 'Mês/Ano',
        'mensalista.index.client': 'Cliente',
        'mensalista.index.area': 'Área',
        'mensalista.index.allClients': 'Todos',
        'mensalista.index.selectArea': 'Selecionar',
        'mensalista.index.viewButton': 'Visualizar',
        'mensalista.index.generateCSV': 'Gerar CSV',
        'mensalista.search.title': 'Resultados',
        'mensalista.search.subtitle': 'Análise detalhada de mensalistas',
        'mensalista.search.colGrossMonthly': 'Mensal Bruto',
        'mensalista.search.colTaxes': 'Tributos',
        'mensalista.search.colPartnerComm': 'Com. Parceiro',
        'mensalista.search.colPartnerComm': 'Com. Sócio',
        'mensalista.search.colNetMonthly': 'Mensal Liq',
        'mensalista.search.colPercentage': '%',
        'mensalista.search.colGrossArea': 'Valor Área Bruto',
        'mensalista.search.colNetArea': 'Valor Área Liq',
        'mensalista.search.colAction': 'Ação',
        'mensalista.result.month.title': 'Resultados do Mês',
        'mensalista.result.month.subtitle': 'Análise de desempenho mensal',
        'mensalista.result.average.title': 'Média 3 Últimos Meses',
        'mensalista.result.average.subtitle': 'Análise de desempenho médio trimestral',
        'mensalista.result.accumulated.title': 'Acumulado 3 Últimos Meses',
        'mensalista.result.accumulated.subtitle': 'Análise de desempenho acumulado trimestral',
        'mensalista.result.colTotalHours': 'Horas Totais',
        'mensalista.result.colGrossTech': 'Hora Técnica Bruta',
        'mensalista.result.colNetTech': 'Hora Técnica Líquida',
        'mensalista.result.colGrossResult': 'Resultado Bruto',
        'mensalista.result.colNetResult': 'Resultado Líquido',
    },
    
    en: {
        // ... traduções existentes do menu, home, reports ...
        
        // ========== COMMON KEYS ==========
        'common.actions': 'Actions',
        'common.save': 'Save',
        'common.saveChanges': 'Save Changes',
        'common.cancel': 'Cancel',
        'common.delete': 'Delete',
        'common.confirmDelete': 'Confirm Deletion',
        'common.edit': 'Edit',
        'common.details': 'Details',
        'common.back': 'Back',
        'common.backToList': 'Back to List',
        'common.search': 'Search',
        'common.new': 'New',
        'common.showing': 'Showing',
        'common.of': 'of',
        'common.records': 'records',
        'common.deleteWarning': 'Are you sure you want to delete?',
        'common.cannotUndo': 'This action cannot be undone',
        'common.allDataRemoved': 'All associated data will be permanently removed from the system',
        'common.completeInfo': 'Complete information',
        'common.updateInfo': 'Update information',
        'common.name': 'Name',
        'common.email': 'Email',
        'common.phone': 'Phone',
        'common.document': 'Document',
        'common.client': 'Client',
        'common.department': 'Department',
        'common.value': 'Value',
        'common.percentage': 'Percentage',
        
        // ========== ATTORNEYS (USERS) ==========
        'attorneys.index.title': 'Users',
        'attorneys.index.subtitle': 'Manage system users',
        'attorneys.index.newButton': 'New User',
        'attorneys.index.searchPlaceholder': 'Search by name, email or login...',
        'attorneys.index.colName': 'Name',
        'attorneys.index.colEmail': 'Email',
        'attorneys.index.colProfile': 'Profile',
        'attorneys.index.colDepartment': 'Department',
        'attorneys.details.title': 'User Details',
        'attorneys.details.subtitle': 'Complete information',
        'attorneys.details.basicInfo': 'Basic Information',
        'attorneys.details.contactInfo': 'Contact Information',
        'attorneys.details.preferences': 'Preferences',
        'attorneys.details.birthDate': 'Birth Date',
        'attorneys.details.login': 'Login',
        'attorneys.edit.title': 'Edit User',
        'attorneys.edit.subtitle': 'Update user information',
        'attorneys.edit.basicInfoSection': 'Basic Information',
        'attorneys.edit.contactSection': 'Contact',
        'attorneys.edit.accessSection': 'Access',
        'attorneys.edit.preferencesSection': 'Preferences',
        'attorneys.delete.title': 'Delete User',
        'attorneys.delete.warning': 'Are you sure you want to delete this user?',
        'attorneys.delete.allData': 'All data associated with this user will be permanently removed from the system',
        
        // ========== CLIENTS ==========
        'clients.index.title': 'Clients',
        'clients.index.subtitle': 'Manage your clients',
        'clients.index.newButton': 'New Client',
        'clients.index.searchPlaceholder': 'Search by name, email, document or phone...',
        'clients.index.colName': 'Name',
        'clients.index.colEmail': 'Email',
        'clients.index.colDocument': 'Document',
        'clients.index.colPhone': 'Phone',
        'clients.index.colRequester': 'Requester',
        'clients.index.colStatus': 'Status',
        'clients.index.statusActive': 'Active',
        'clients.index.statusInactive': 'Inactive',
        'clients.index.statusInternal': 'Internal',
        'clients.details.title': 'Client Details',
        'clients.details.subtitle': 'Complete information',
        'clients.details.contactInfo': 'Contact Information',
        'clients.details.documentation': 'Documentation',
        'clients.details.requester': 'Requester',
        'clients.edit.title': 'Edit Client',
        'clients.edit.subtitle': 'Update client information',
        'clients.edit.basicInfo': 'Basic Information',
        'clients.edit.contact': 'Contact',
        'clients.edit.additionalInfo': 'Additional Information',
        'clients.edit.internalClient': 'Internal Client',
        'clients.edit.internalClientDesc': 'Check if internal company client',
        'clients.edit.inactiveClient': 'Inactive Client',
        'clients.edit.inactiveClientDesc': 'Check to deactivate this client',
        'clients.edit.changeImage': 'Change Image',
        'clients.edit.imageHint': 'PNG, JPG or GIF (max. 5MB)',
        'clients.delete.title': 'Delete Client',
        'clients.delete.warning': 'Are you sure you want to delete this client?',
        
        // ... Continue com as outras seções em inglês ...
        // (Por brevidade, não vou repetir todas, mas o padrão é o mesmo)
    },
    
    es: {
        // ... traduções existentes do menu, home, reports ...
        
        // ========== CLAVES COMUNES ==========
        'common.actions': 'Acciones',
        'common.save': 'Guardar',
        'common.saveChanges': 'Guardar Cambios',
        'common.cancel': 'Cancelar',
        'common.delete': 'Eliminar',
        'common.confirmDelete': 'Confirmar Eliminación',
        'common.edit': 'Editar',
        'common.details': 'Detalles',
        'common.back': 'Volver',
        'common.backToList': 'Volver a la Lista',
        'common.search': 'Buscar',
        'common.new': 'Nuevo',
        'common.showing': 'Mostrando',
        'common.of': 'de',
        'common.records': 'registros',
        'common.deleteWarning': '¿Está seguro de que desea eliminar?',
        'common.cannotUndo': 'Esta acción no se puede deshacer',
        'common.allDataRemoved': 'Todos los datos asociados serán eliminados permanentemente del sistema',
        'common.completeInfo': 'Información completa',
        'common.updateInfo': 'Actualizar información',
        'common.name': 'Nombre',
        'common.email': 'Correo',
        'common.phone': 'Teléfono',
        'common.document': 'Documento',
        'common.client': 'Cliente',
        'common.department': 'Área',
        'common.value': 'Valor',
        'common.percentage': 'Porcentaje',
        
        // ========== ATTORNEYS (USUARIOS) ==========
        'attorneys.index.title': 'Usuarios',
        'attorneys.index.subtitle': 'Gestionar usuarios del sistema',
        'attorneys.index.newButton': 'Nuevo Usuario',
        'attorneys.index.searchPlaceholder': 'Buscar por nombre, correo o login...',
        'attorneys.index.colName': 'Nombre',
        'attorneys.index.colEmail': 'Correo',
        'attorneys.index.colProfile': 'Perfil',
        'attorneys.index.colDepartment': 'Área',
        'attorneys.details.title': 'Detalles del Usuario',
        'attorneys.details.subtitle': 'Información completa',
        'attorneys.details.basicInfo': 'Información Básica',
        'attorneys.details.contactInfo': 'Información de Contacto',
        'attorneys.details.preferences': 'Preferencias',
        'attorneys.details.birthDate': 'Fecha de Nacimiento',
        'attorneys.details.login': 'Login',
        'attorneys.edit.title': 'Editar Usuario',
        'attorneys.edit.subtitle': 'Actualizar información del usuario',
        'attorneys.edit.basicInfoSection': 'Información Básica',
        'attorneys.edit.contactSection': 'Contacto',
        'attorneys.edit.accessSection': 'Acceso',
        'attorneys.edit.preferencesSection': 'Preferencias',
        'attorneys.delete.title': 'Eliminar Usuario',
        'attorneys.delete.warning': '¿Está seguro de que desea eliminar este usuario?',
        'attorneys.delete.allData': 'Todos los datos asociados a este usuario serán eliminados permanentemente del sistema',
        
        // ... Continue com as outras seções em espanhol ...
        // (Por brevidade, não vou repetir todas, mas o padrão é o mesmo)
    }
};
```

### Passo 2: Exemplo Prático - Attorneys/Index.cshtml

Vou mostrar como ficaria a view com as traduções. Você precisa adicionar `data-i18n` nos elementos de texto:

**ANTES (sem tradução):**
```html
<h1 class="page-title">Usuários</h1>
<p class="page-subtitle">Gerencie os usuários do sistema</p>
```

**DEPOIS (com tradução):**
```html
<h1 class="page-title" data-i18n="attorneys.index.title">Usuários</h1>
<p class="page-subtitle" data-i18n="attorneys.index.subtitle">Gerencie os usuários do sistema</p>
```

## 🎯 Próximos Passos

1. **Copie as chaves de tradução** acima e adicione no objeto `translations` do `_Layout.cshtml`
2. **Modifique uma view por vez** adicionando `data-i18n` nos elementos
3. **Teste** clicando nas bandeiras 🇧🇷 🇺🇸 🇪🇸
4. **Repita** para as outras views

## ⚠️ Importante

- Mantenha o texto original em português no HTML (para fallback)
- Use chaves descritivas e hierárquicas
- Teste cada módulo antes de passar para o próximo
- As traduções em EN e ES acima estão incompletas por brevidade - você precisará completá-las

## 📞 Dúvidas?

Se precisar de ajuda com alguma view específica ou tiver dúvidas sobre como traduzir algum elemento, é só perguntar!
