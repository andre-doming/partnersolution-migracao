<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Importar.aspx.cs" Inherits="SolutionsTools.UI.Importar" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1>Importar clientes (CSV) <a href="/sample/modelo.csv" class="btn btn-outline-dark btn-sm">Download Modelo</a> </h1>

    <div class="form-group">
        <div class="input-group">
            <select class="custom-select" id="mselEmpresas" runat="server"></select>
            <div class="input-group-append">
                <asp:Button ID="btnListar" runat="server" Text="Listar" class="btn btn-outline-secondary" Visible="false" />
            </div>
        </div>
    </div>

    <div class="form-group">
        <div class="input-group">
            <div class="custom-file">
                <asp:FileUpload ID="fupClientes" runat="server" class="custom-file-input" aria-describedby="fupClientes" />
                <label class="custom-file-label" for="myInput">Escolha o arquivo</label>
            </div>
            <asp:Button ID="btnLerArquivo" runat="server" Text="Abrir" OnClick="btnLerArquivo_Click" class="btn btn-primary" />
        </div>
    </div>

    <div runat="server" id="divExportReport" style="text-align: right" visible="false">
        <a href="/importar.aspx?downloadReportErrors=true" class="btn btn-link btn-sm">Download Report Errors</a>
    </div>

    <div class="form-row">
        <div class="col-md-12 mb-3" style="min-height: 100px; max-height: 550px; overflow-x: hidden; overflow-y: auto">
            <asp:GridView ID="grvClientes" runat="server" AutoGenerateColumns="false" DataKeyNames="SeqId,Cpf,Email,Acao"
                Style="width: 99.8%; height: 99%;" CssClass="table table-hover table-striped table-bordered table-condensed table-sm" 
                HeaderStyle-CssClass="thead-light" GridLines="None" >
                <Columns>
                    <asp:TemplateField>
                        <HeaderTemplate>
                            <div style="text-align: center">
                                <input id="chkSelecionarTodos" type="checkbox" checked />
                            </div>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <div style="text-align: center">
                                <input id="chkSelecionado" runat="server" type="checkbox" checked />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="Nome" HeaderText="Name"></asp:BoundField>
                    <asp:BoundField DataField="Sobrenome" HeaderText="Sobrenome"></asp:BoundField>
                    <asp:BoundField DataField="Cpf" HeaderText="CPF"></asp:BoundField>
                    <asp:BoundField DataField="Email" HeaderText="Email"></asp:BoundField>
                    <asp:BoundField DataField="Departamento" HeaderText="Depto."></asp:BoundField>
                    <asp:BoundField DataField="Cargo" HeaderText="Cargo"></asp:BoundField>
                    <asp:BoundField DataField="Acao" HeaderText="Ação"></asp:BoundField>
                </Columns>
            </asp:GridView>
        </div>
    </div>

    <div class="form-row">
        <div class="col-md-2 mb-3">
            <asp:Button ID="btnProcessar" runat="server" Text="Processar" OnClick="btnProcessar_Click" Visible="false" class="btn btn-primary mb-2" />
        </div>
    </div>

    <asp:HiddenField ID="Funcao" runat="server" Value="funcImpCsv" />

    <script type="text/javascript">
        $("#chkSelecionarTodos").click(function () {
            $('input:checkbox').prop('checked', this.checked);
        });

        document.querySelector('.custom-file-input').addEventListener('change', function (e) {
            var fileName = document.getElementById("<%=fupClientes.ClientID %>").files[0].name;
            var nextSibling = e.target.nextElementSibling
            nextSibling.innerText = fileName
        })

    </script>
</asp:Content>
