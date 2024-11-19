
$('#table-SearchRecord').DataTable({
    "ordering": false,
    "paging": true,
    "searching": true,
    "oLanguage": {
        "sEmptyTable": "Nenhum registro encontrado na tabela",
        "sInfo": "Mostrar _START_ até _END_ de _TOTAL_ registros",
        "sInfoEmpty": "Mostrar 0 até 0 de 0 Registros",
        "sInfoFiltered": "(Filtrar de _MAX_ total registros)",
        "sInfoPostFix": "",
        "sInfoThousands": ".",
        "sLengthMenu": "Mostrar _MENU_ registros por pagina",
        "sLoadingRecords": "Carregando...",
        "sProcessing": "Processando...",
        "sZeroRecords": "Nenhum registro encontrado",
        "sSearch": "Pesquisar",
        "oPaginate": {
            "sNext": "Proximo",
            "sPrevious": "Anterior",
            "sFirst": "Primeiro",
            "sLast": "Ultimo"
        },
        "oAria": {
            "sSortAscending": ": Ordenar colunas de forma ascendente",
            "sSortDescending": ": Ordenar colunas de forma descendente"
        }
    }
});
$('.close-alert').click(function () {
    $('.alert').hide('hide');
});
$('#table-SearchRecords').DataTable({
    "serverSide": true,
    "processing": true,
    "ajax": {
        "url": "/ProcessRecords/GetProcessRecords",
        "type": "POST",
        "dataType": "json",
        "dataSrc": function (json) {
            console.log("Resposta recebida do servidor:", json);
            return json.data;
        },
        "error": function (xhr, error, thrown) {
            console.error("Erro no DataTables:", xhr.responseText);
        }
    },
    "columns": [
        {
            "data": "date",
            "title": "Data",
            "render": function (data, type, row) {
                if (!data) return ''; // Caso o campo seja nulo ou indefinido
                let dateObj = new Date(data); // Cria um objeto Date a partir da string ISO
                let dia = String(dateObj.getDate()).padStart(2, '0'); // Dia com 2 dígitos
                let mes = String(dateObj.getMonth() + 1).padStart(2, '0'); // Mês com 2 dígitos (0 indexado)
                let ano = dateObj.getFullYear(); // Ano completo
                return `${dia}/${mes}/${ano}`; // Retorna no formato dd/MM/yyyy
            }
        },
        { "data": "horaInicial", "title": "Hora Inicial" },
        { "data": "horaFinal", "title": "Hora Final" },
        { "data": "horas", "title": "Horas" },
        { "data": "cliente", "title": "Cliente" },
        { "data": "usuario", "title": "Usuário" },
        { "data": "tipo", "title": "Tipo" },
        {
            "data": null,
            "render": function (data, type, row) {
                let editBtn = data.editLink
                    ? `<a href="${data.editLink}" class="btn btn-outline-secondary btn-sm"><i class="fas fa-edit fa-xs"></i></a>`
                    : `<a class="btn btn-outline-secondary btn-sm disabled"><i class="fas fa-edit fa-xs"></i></a>`;
                let detailsBtn = `<a href="${data.detailsLink}" class="btn btn-outline-secondary btn-sm"><i class="fas fa-info-circle fa-xs"></i></a>`;
                let deleteBtn = data.deleteLink
                    ? `<a href="${data.deleteLink}" class="btn btn-outline-secondary btn-sm"><i class="fas fa-trash-alt fa-xs"></i></a>`
                    : `<a class="btn btn-outline-secondary btn-sm disabled"><i class="fas fa-trash-alt fa-xs"></i></a>`;
                return `${editBtn} ${detailsBtn} ${deleteBtn}`;
            },
            "orderable": false
        }
    ],
    "language": {
        "sEmptyTable": "Nenhum registro encontrado",
        "sInfo": "Mostrando de _START_ até _END_ de _TOTAL_ registros",
        "sInfoEmpty": "Mostrando 0 até 0 de 0 registros",
        "sInfoFiltered": "(Filtrados de _MAX_ registros)",
        "sInfoPostFix": "",
        "sInfoThousands": ".",
        "sLengthMenu": "_MENU_ resultados por página",
        "sLoadingRecords": "Carregando...",
        "sProcessing": "Processando...",
        "sZeroRecords": "Nenhum registro encontrado",
        "sSearch": "Pesquisar",
        "oPaginate": {
            "sNext": "Próximo",
            "sPrevious": "Anterior",
            "sFirst": "Primeiro",
            "sLast": "Último"
        },
        "oAria": {
            "sSortAscending": ": Ordenar colunas de forma ascendente",
            "sSortDescending": ": Ordenar colunas de forma descendente"
        },
        "select": {
            "rows": {
                "_": "Selecionado %d linhas",
                "0": "Nenhuma linha selecionada",
                "1": "Selecionado 1 linha"
            }
        },
        "buttons": {
            "copy": "Copiar para a área de transferência",
            "copyTitle": "Cópia bem sucedida",
            "copySuccess": {
                "1": "Uma linha copiada com sucesso",
                "_": "%d linhas copiadas com sucesso"
            }
        }
    }
});
