
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
        "data": function (d) {
            // Adiciona parâmetros de pesquisa e ordenação
            d.search = d.search.value; // Valor da pesquisa global
            d.orderColumn = d.order[0].column; // Índice da coluna para ordenar
            d.orderDir = d.order[0].dir; // Direção da ordenação (asc ou desc)
        }
    },
    "columns": [
        { "data": "date", "title": "Data" },
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
        "sSearch": "Pesquisar:"
    }
});
