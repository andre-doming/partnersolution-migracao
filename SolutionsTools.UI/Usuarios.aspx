<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Usuarios.aspx.cs" Inherits="SolutionsTools.UI.Usuarios" MaintainScrollPositionOnPostback="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    ​<%@ Register Src="~/Controls/acUsuarios.ascx" TagName="ModalUser" TagPrefix="md" %>

    <h1>Usuários</h1>

    <div class="form-group">
        <div class="input-group">
            <select class="custom-select" id="mselEmpresas" runat="server"> </select>
            <div class="input-group-append">
                <asp:Button ID="btnListar" runat="server" Text="Listar" OnClick="btnListar_Click" class="btn btn-outline-secondary" />
            </div>
        </div>
    </div>

    <div class="form-group">
        <div class="input-group">
            <select class="custom-select" id="mselProcurarPor" runat="server" >
                    <option value="Email">Email</option>
                    <option value="Nome">Nome</option>
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
                Usuário
            </button>
        </div>
    </div>

    <div id="divGrv" style="height: 600px; overflow-x: hidden; overflow-y: auto" onscroll="SetDivPosition();" class="form-group">
        <asp:GridView ID="grvUsuarios" runat="server" Height="410px" AutoGenerateColumns="False" DataKeyNames="Id,Login"
            Style="width: 99.8%; height: 99%;" CssClass="table table-hover table-striped table-bordered table-condensed table-sm" HeaderStyle-CssClass="thead-light" GridLines="None"
            OnRowCommand="grvUsuarios_RowCommand" OnRowDataBound="grvUsuarios_RowDataBound"  AllowPaging="true" OnPageIndexChanging="grvUsuarios_PageIndexChanging" PageSize="10">
            <HeaderStyle CssClass="thead-light"></HeaderStyle>
            <Columns>
                <asp:TemplateField ItemStyle-HorizontalAlign="Center">
                    <ItemTemplate>
                        <asp:ImageButton runat="server" ID="ibtnEdit" CommandName="Upd" CommandArgument="<%# Container.DataItemIndex %>" ImageUrl="~/Img/edit.png" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField ItemStyle-HorizontalAlign="Center">
                    <ItemTemplate>
                        <asp:ImageButton runat="server" ID="ibtnDelete" CommandName="Del" CommandArgument="<%# Container.DataItemIndex %>" ImageUrl="~/Img/delete.png" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Nome" HeaderText="Nome"></asp:BoundField>
                <asp:BoundField DataField="Login" HeaderText="Login"></asp:BoundField>
                <asp:BoundField DataField="Email" HeaderText="Email"></asp:BoundField>
                <asp:BoundField DataField="Primeiro_Acesso" HeaderText="Primeiro acesso?"></asp:BoundField>   
                <asp:BoundField DataField="Acesso_Token" HeaderText="É token?"></asp:BoundField>                
                <asp:BoundField DataField="Admin" HeaderText="Admin?"></asp:BoundField>
                <asp:BoundField DataField="Ativo" HeaderText="Ativo"></asp:BoundField>
                <asp:TemplateField ItemStyle-HorizontalAlign="Center">
                    <ItemTemplate>
                        <asp:ImageButton runat="server" ID="ibtnRecuperarSenha" CommandName="Recovery" CommandArgument="<%# Container.DataItemIndex %>" ImageUrl="~/Img/locker.png" />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>Não foi encontrado usuário(s)</EmptyDataTemplate>
        </asp:GridView>
    </div>

    <asp:HiddenField ID="Funcao" runat="server" Value="funcUsuarios" />

    <md:ModalUser ID="acModalUser" runat="server" />

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
