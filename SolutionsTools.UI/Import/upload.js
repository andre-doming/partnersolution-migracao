$(document).ready(function () {
    $('#btnEnviar').click(function (evt) {
        doIncrementProgressBar(0);

        $('#divExportReport').hide();

        var fileUpload = $("#fupClientes").get(0);
        var files = fileUpload.files;

        var data = new FormData();
        for (var i = 0; i < files.length; i++) {
            data.append(files[i].name, files[i]);
        }

        var options = {};
        options.url = "FileUploadHandler.ashx";
        options.type = "POST";
        options.data = data;
        options.contentType = false;
        options.processData = false;
        options.uploadProgress = function (event, position, total, percentComplete) {
            doIncrementProgressBar(percentComplete);
        };
        options.success = function (data) {
            doIncrementProgressBar(100);
            readFile(data);
            $("#btnProcessar").show();
        };
        options.error = function () {
            $('#resposta').html('Erro ao enviar requisição!!!');
            $("#btnProcessar").hide();
        };

        $.ajax(options);

        evt.preventDefault();
    });

    document.querySelector('.custom-file-input').addEventListener('change', function (e) {
        var fileName = document.getElementById("fupClientes").files[0].name;
        var nextSibling = e.target.nextElementSibling
        nextSibling.innerText = fileName
    })
});

function readFile(file) {
    var data;
    completeFile = baseUrl() + "uploads/" + file;
    $.ajax({
        type: "GET",
        url: completeFile,
        dataType: "text",
        success: function (response) {
            data = $.csv.toArrays(response, {
                delimiter: "'",
                separator: ';',
                headers: true
            });
            generateHtmlTable(data);
        }
    })
}

function generateHtmlTable(data) {
    var html = '<table id="csv-table" class="table table-condensed table-hover table-striped" Style="width: 99.8%; height: 99%;">';

    if (typeof (data[0]) === 'undefined') {
        return null;
    } else {
        $('#csv-display').html("");
        $.each(data, function (index, row) {
            if (index == 0) {
                html += '<thead>';
                html += '<tr>';
                $.each(row, function (index, colData) {
                    if (index == 0) {
                        html += insertCheckBoxColumn(true);
                    }
                    if (showColumns(index) === "S") {
                        html += '<th>';
                        html += getColumnsNames(index);
                        html += '</th>';
                    } else {
                        html += '<th style="display: none">';
                        html += getColumnsNames(index);
                        html += '</th>';
                    }
                });
                html += '</tr>';
                html += '</thead>';
                html += '<tbody>';
            } else {
                html += '<tr>';
                $.each(row, function (index, colData) {
                    if (index == 0) {
                        html += insertCheckBoxColumn(false);
                    }
                    if (showColumns(index) === "S") {
                        html += '<td>';
                        html += colData;
                        html += '</td>';
                    } else {
                        html += '<td style="display: none">';
                        html += colData
                        html += '</td>';
                    }
                });
                html += '</tr>';
            }
        });
        html += '</tbody>';
        html += '</table>';
        $('#csv-display').html(html);
    }
}

function insertCheckBoxColumn(isHeader) {
    if (isHeader)
        return "<th><input type='checkbox' id='chkSelecionarTodos' class='chkSelecionarTodos' checked onclick='checkedAllCheckBox();' /></th>";
    else
        return "<td><input type='checkbox' id='chkSelecionado' class='chkSelecionado' checked /></td>";
}

function checkedAllCheckBox() {
    if ($('.chkSelecionado').attr('checked') == 'checked') {
        $('.chkSelecionado').prop('checked', '');
    }
    else {
        $('.chkSelecionado').prop('checked', 'checked');
    }
}

function showColumns(id) {
    var configs = "1|nome|Nome|S;2|sobrenome|Sobrenome|S;3|cpf|CPF|S;4|email|E-mail|S;5|sexo|Sexo|N;6|dt_nascimento|Dt.Nasc|N;7|departamento|Depto.|S;8|cargo|Cargo|S;9|acao|Ação|S";

    var colunas = configs.split(";");

    var atributos = colunas[id].split("|");

    return atributos[3];
}

function getColumnsNames(id) {
    var configs = "1|nome|Nome|S;2|sobrenome|Sobrenome|S;3|cpf|CPF|S;4|email|E-mail|S;5|sexo|Sexo|N;6|dt_nascimento|Dt.Nasc|N;7|departamento|Depto.|S;8|cargo|Cargo|S;9|acao|Ação|S";

    var colunas = configs.split(";");

    var atributos = colunas[id].split("|");

    return atributos[2];
}
