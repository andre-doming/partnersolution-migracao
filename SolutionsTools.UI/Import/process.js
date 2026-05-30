$(document).ready(function () {
    $('#btnProcessar').click(function (evt) {
        doIncrementProgressBar(0);

        $('#divExportReport').hide();

        $("#csv-table tbody tr input[type=checkbox]:checked").each(function () {
            var row = $(this).closest("tr").children('td');
            processRows(row);
        });

        evt.preventDefault();
    });
});

function processRows(row) {

    var empresa = $(".mselEmpresas option:selected").text();
    var empresaId = $(".mselEmpresas").val();

    var formData = new FormData();

    formData.append("Empresa", empresa);
    formData.append("EmpresaId", empresaId);
    formData.append("Nome", row.eq(1).text());
    formData.append("Sobrenome", row.eq(2).text());
    formData.append("Cpf", row.eq(3).text());
    formData.append("Email", row.eq(4).text());
    formData.append("Sexo", row.eq(5).text());
    formData.append("Dt_Nascimento", row.eq(6).text());
    formData.append("Departamento", row.eq(7).text());
    formData.append("Cargo", row.eq(8).text());
    formData.append("Acao", row.eq(9).text());

    var options = {};
    options.url = "FileProcessHandler.ashx";
    options.type = "POST";
    options.data = formData;
    options.contentType = false;
    options.processData = false;
    options.uploadProgress = function (event, position, total, percentComplete) {
        doIncrementProgressBar(percentComplete);
    };
    options.success = function (data, x, y) {
        doIncrementProgressBar(100);
        if (data.includes('sucesso')) {
            row.css('background-color', '#B8D4E0');
        }
        else {
            row.css('background-color', '#FBCFD0');
            $('#divExportReport').show();
        }
        row.prop('title', data);
    };
    options.error = function (data, x, y) {
        doIncrementProgressBar(100);
        row.prop('title', data);
        row.css('background-color', '#FBCFD0');
        $('#divExportReport').show();
    };

    $.ajax(options);
}

function exportLogError() {
    var log = "";

    $('#csv-table tbody tr').each((tr_idx, tr) => {
        var row = $(tr).children('td');

        var toolTip = row.prop('title');

        if (toolTip.length > 0) {
            if (!toolTip.includes("sucesso")) {

                var now = new Date(Date.now());

                var linha = "";
                linha += (tr_idx + 1) + ";";
                linha += row.eq(1).text() + ";";
                linha += row.eq(2).text() + ";";
                linha += toolTip + ";";
                linha += now.toISOString() + "\n";

                log += linha;
            }
        }
    });

    var file = "Log_" + Date.now() + ".csv";

    var blob = new Blob([log], { type: "text/plain;charset=utf-8" });
    saveAs(blob, file);
}
