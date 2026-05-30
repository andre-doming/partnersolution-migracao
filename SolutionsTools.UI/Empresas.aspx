<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Empresas.aspx.cs" Inherits="SolutionsTools.UI.Empresas" MaintainScrollPositionOnPostback="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    ​<%@ Register Src="~/Controls/acEmpresas.ascx" TagName="ModalCompany" TagPrefix="md" %>
    ​<%@ Register Src="~/Controls/acExcluirEmpresas.ascx" TagName="ModalDeleteCompany" TagPrefix="md" %>

    <h1>Empresas Parceiras</h1>

    <div class="form-group" style="display: none">
        <div class="input-group">
            <select class="custom-select" id="mselEmpresas" runat="server" > </select>
            <div class="input-group-append">
                <asp:Button ID="btnListar" runat="server" Text="Listar" OnClick="btnListar_Click" class="btn btn-outline-secondary" />
            </div>
        </div>
    </div>

    <div class="form-group">
        <div class="input-group">
            <select class="custom-select" id="mselProcurarPor" runat="server" >
                    <option value="CNPJ">CNPJ</option>
                    <option value="Nome_Fantasia">Nome Fantasia</option>
            </select>
            <asp:TextBox ID="txtProcurarPor" runat="server" Width="80%" class="custom-input"></asp:TextBox>
            <div class="input-group-append">
                <asp:Button ID="btnProcurarPor" runat="server" Text="Listar" OnClick="btnListar_Click" class="btn btn-outline-secondary" />
            </div>
        </div>
    </div>

    <div class="form-group">
        <div class="input-group">
            <button type="button" class="btn btn-primary" runat="server" id="btnAdicionar" onserverclick="btnAdicionar_Click">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-plus-circle-fill" viewBox="0 0 16 16">
                    <path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zM8.5 4.5a.5.5 0 0 0-1 0v3h-3a.5.5 0 0 0 0 1h3v3a.5.5 0 0 0 1 0v-3h3a.5.5 0 0 0 0-1h-3v-3z" />
                </svg>
                Empresa
            </button>
        </div>
    </div>

    <div id="divGrv" style="height: 600px; overflow-x: hidden; overflow-y: auto" onscroll="SetDivPosition();" class="form-group">
        <asp:GridView ID="grvEmpresas" runat="server" Height="410px" AutoGenerateColumns="False" DataKeyNames="Id"
            Style="width: 99.8%; height: 99%;" CssClass="table table-hover table-striped table-bordered table-condensed table-sm" HeaderStyle-CssClass="thead-light" GridLines="None"
            OnRowCommand="grvEmpresas_RowCommand" OnRowDataBound="grvEmpresas_RowDataBound" AllowPaging="true" OnPageIndexChanging="grvEmpresas_PageIndexChanging" PageSize="10">
            <HeaderStyle CssClass="thead-light"></HeaderStyle>
            <Columns>
                <asp:TemplateField ItemStyle-HorizontalAlign="Center">
                    <ItemTemplate>
                        <asp:ImageButton runat="server" ID="ibtnUpd" CommandName="Upd" CommandArgument="<%# Container.DataItemIndex %>" ImageUrl="~/Img/edit.png" ToolTip="Editar dados da empresa" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField ItemStyle-HorizontalAlign="Center">
                    <ItemTemplate>
                        <asp:ImageButton runat="server" ID="ibtnDel" CommandName="Del" CommandArgument="<%# Container.DataItemIndex %>" ImageUrl="~/Img/delete.png" ToolTip="Excluir empresa" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Nome_Fantasia" HeaderText="Nome Fantasia"></asp:BoundField>
                <asp:BoundField DataField="Gerente_Responsavel" HeaderText="Gerente Responsável"></asp:BoundField>
                <asp:BoundField DataField="CNPJ" HeaderText="CNPJ"></asp:BoundField>
                <asp:BoundField DataField="Ativo" HeaderText="Ativo"></asp:BoundField>
            </Columns>
            <EmptyDataTemplate>Não foi encontrado empresa(s)</EmptyDataTemplate>
        </asp:GridView>
    </div>

    <asp:HiddenField ID="Funcao" runat="server" Value="funcEmpresas" />

    <md:ModalCompany ID="acModalCompany" runat="server" />
    <md:ModalDeleteCompany ID="acModalDeleteCompany" runat="server" />

    <script type="text/javascript">
        window.onload = function () {
            var strCook = document.cookie;
            if (strCook.indexOf("!~") != 0) {
                var intS = strCook.indexOf("!~");
                var intE = strCook.indexOf("~!");
                var strPos = strCook.substring(intS + 2, intE);
                document.getElementById("divGrv").scrollTop = strPos;
            }
        }
        function SetDivPosition() {
            var intY = document.getElementById("divGrv").scrollTop;
            document.cookie = "yPos=!~" + intY + "~!";
        }

        function RestartDivPosition() {
            document.cookie = "yPos=!~0~!";
        }
    </script>
</asp:Content>
