<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" EnableViewState="false" AutoEventWireup="true" CodeBehind="ImportarNew.aspx.cs" Inherits="SolutionsTools.UI.ImportarNew" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <script src="Import/jquery.form.js" type="text/javascript"></script>
    <script src="Import/utils.js" type="text/javascript"></script>
    <script src="Import/upload.js" type="text/javascript"></script>
    <script src="Import/process.js" type="text/javascript"></script>
    <script src="Import/jquery.csv-0.71.min.js"></script>
    <script src="Import/FileSaver.js"></script>

    <h1>Importar clientes new (CSV) <a href="/sample/modelo.csv" class="btn btn-outline-dark btn-sm">Download Modelo</a> </h1>

    <div class="form-group">
        <div class="input-group">
            <select class="custom-select mselEmpresas" id="mselEmpresas" runat="server"></select>
            <div class="input-group-append">
                <asp:Button ID="btnListar" runat="server" Text="Listar" class="btn btn-outline-secondary" Visible="false" />
            </div>
        </div>
    </div>

    <div class="form-group">
        <div class="input-group">
            <div class="custom-file">
                <input type="file" name="fupClientes" id="fupClientes" class="custom-file-input" aria-describedby="fupClientes" />
                <label class="custom-file-label" for="myInput">Escolha o arquivo</label>
            </div>
            <input type="button" id="btnEnviar" value="Abrir" class="btn btn-primary mb-2" />
        </div>
        <div class="progress">
            <div id="progressBar" class="progress-bar" role="progressbar" style="width: 0%;" aria-valuenow="25" aria-valuemin="0" aria-valuemax="100"><span id="progressPercent">0</span></div>
        </div>
        <div id="resposta"></div>
    </div>

    <div id="divExportReport" style="text-align: right; display: none">
        <a href="#" class="btn btn-link btn-sm" onclick="exportLogError();">Download Report Errors</a>
    </div>

    <div class="form-row">
        <div id="csv-display" class="col-md-12 mb-3" style="min-height: 100px; max-height: 550px; overflow-x: hidden; overflow-y: auto">
        </div>
    </div>

    <div class="form-row">
        <div class="col-md-2 mb-3">
            <input type="button" id="btnProcessar" value="Processar" class="btn btn-primary mb-2" style="display: none" />
        </div>
    </div>

    <div id="divProcessando" class="divProcessando" style="display: none"></div>

</asp:Content>
