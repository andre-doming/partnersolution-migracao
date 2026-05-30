<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Clientes.aspx.cs" Inherits="SolutionsTools.UI.Clientes" MaintainScrollPositionOnPostback="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    ​<%@ Register Src="~/Controls/acClientes.ascx" TagName="ModalClient" TagPrefix="md" %>
    ​<%@ Register Src="~/Controls/acExcluirClientes.ascx" TagName="ModalDeleteClient" TagPrefix="md" %>

    <h1>Clientes</h1>

    <div class="form-group">
        <div class="input-group">
            <select class="custom-select" id="mselEmpresas" runat="server"> </select>
            <div class="input-group-append">
                <asp:Button ID="btnListar" runat="server" Text="Listar" OnClick="btnListar_Click" class="btn btn-outline-secondary" Visible="false" />
            </div>
        </div>
    </div>

    <div class="form-group">
        <div class="input-group">
            <select class="custom-select" id="mselProcurarPor" runat="server" >
                    <option value="CPF">CPF</option>
                    <option value="Nome_Completo">Nome</option>
            </select>
            <asp:TextBox ID="txtProcurarPor" runat="server" Width="80%" class="custom-input"></asp:TextBox>
            <div class="input-group-append">
                <asp:Button ID="btnProcurarPor" runat="server" Text="Listar" OnClick="btnListar_Click" class="btn btn-outline-secondary" />
            </div>
        </div>
    </div>


   <div class="form-group">
        <button type="button" class="btn btn-primary" runat="server" id="btnExcluir" onserverclick="btnExcluir_Click">Excluir</button>
        <button type="button" class="btn btn-primary" runat="server" id="btnSincronizar" onserverclick="btnSincronizar_Click">Sincronizar</button>
   </div>
   
    <div class="form-row">
        <div id="divGrv" class="col-md-12 mb-3" style="height: 600px; overflow-x: hidden; overflow-y: auto" onscroll="SetDivPosition();">
            <asp:GridView ID="grvClientes" runat="server" Height="410px" AutoGenerateColumns="False" DataKeyNames="Id_Cliente,Email"
                Style="width: 99.8%; height: 99%; " CssClass="table table-hover table-striped table-bordered table-condensed table-sm " HeaderStyle-CssClass="thead-light" GridLines="None"
                OnRowCommand="grvClientes_RowCommand" OnRowDataBound="grvClientes_RowDataBound" AllowPaging="true" OnPageIndexChanging="grvClientes_PageIndexChanging" PageSize="10" >
                <HeaderStyle CssClass="thead-light"></HeaderStyle>
                <Columns>
                    <asp:TemplateField>
                        <HeaderTemplate>
                            <div style="text-align: center">
                                <input id="chkSelecionarTodos" type="checkbox" />
                            </div>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <div style="text-align: center">
                                <input id="chkSelecionado" runat="server" type="checkbox" class="checkbox" />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="Nome" HeaderText="Nome" ></asp:BoundField>  
                    <asp:BoundField DataField="Sobrenome" HeaderText="Sobrenome" ></asp:BoundField>  
                    <asp:BoundField DataField="Cpf" HeaderText="CPF" ItemStyle-Width="120px" ItemStyle-CssClass="fixedWidth"></asp:BoundField>  
                    <asp:BoundField DataField="Email" HeaderText="Email" ItemStyle-Wrap="true" ItemStyle-Width="250px" ItemStyle-CssClass="fixedWidth"></asp:BoundField>  
                    <asp:BoundField DataField="Departamento" HeaderText="Depto." ></asp:BoundField>   
                    <asp:BoundField DataField="Cargo" HeaderText="Cargo" ></asp:BoundField>                      
                    <asp:BoundField DataField="EmpresaAnterior" HeaderText="Emp.Anterior" ></asp:BoundField>  
                    <asp:TemplateField ItemStyle-HorizontalAlign="Center" >
                        <ItemTemplate>
                            <asp:ImageButton runat="server" ID="ibtnEdit" CommandName="Upd" CommandArgument="<%# Container.DataItemIndex %>" ImageUrl="~/Img/edit.png" />
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField ItemStyle-HorizontalAlign="Center"> 
                        <ItemTemplate>
                            <asp:ImageButton runat="server" ID="ibtnDelete" CommandName="Del" CommandArgument="<%# Container.DataItemIndex %>" ImageUrl="~/Img/delete.png"  />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>Não foi encontrado cliente(s)</EmptyDataTemplate>
                <PagerStyle HorizontalAlign="Center" />	
            </asp:GridView>
            <asp:HiddenField ID="hdnQtdeCliente" runat="server" Value="0"> </asp:HiddenField>
            <asp:HiddenField ID="hdnQtdePaginas" runat="server" Value="0"> </asp:HiddenField>
            <asp:HiddenField ID="hdnPaginaAtual" runat="server" Value="0"> </asp:HiddenField>
        </div>
    </div>
 
    <asp:HiddenField ID="Funcao" runat="server" Value="funcClientes" />

    <md:ModalClient ID="acModalClient" runat="server" />
    <md:ModalDeleteClient ID="acModalDeleteClient" runat="server" />

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

        $(document).ready(function () {
            $('#chkSelecionarTodos').on('click', function () {
                if (this.checked) {
                    $('.checkbox').each(function () {
                        this.checked = true;
                    });
                } else {
                    $('.checkbox').each(function () {
                        this.checked = false;
                    });
                }
            });

            $('.checkbox').on('click', function () {
                if ($('.checkbox:checked').length == $('.checkbox').length) {
                    $('#chkSelecionarTodos').prop('checked', true);
                } else {
                    $('#chkSelecionarTodos').prop('checked', false);
                }
            });
        });

    </script>
    <style type="text/css">
        .fixedWidth
        {
            word-wrap: break-word;
            word-break: break-all;
        }
    </style>
</asp:Content>
