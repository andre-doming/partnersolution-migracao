<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="acClientes.ascx.cs" Inherits="SolutionsTools.UI.acClientes" %>

<!-- Custom style -->
<link href="../Content/custom-radio.css" rel="stylesheet" />

<!-- Modal -->
<div class="modal fade" id="modalClient" role="dialog">
    
    <div class="modal-dialog modal-dialog-centered modal-lg"">
    
        <!-- Modal content-->
        <div class="modal-content">
            <div class="modal-header">
                <h4 class="modal-title">Dados do cliente</h4>
                <button type="button" class="close" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">

                <asp:HiddenField ID="hdnId" runat="server" />

                <!-- Nome -->
                <div class="row">
                    <div class="col">
                        <div class="form-group">
                        <label class="form-label form-label-sm" for="formNome">Nome</label>
                        <asp:TextBox ID="txtNome" runat="server" class="form-control form-control-sm" ReadOnly="true"></asp:TextBox>
                        </div>
                    </div>
                    <div class="col">
                        <div class="form-group">
                        <label class="form-label form-label-sm" for="formSobrenome">Sobrenome</label>
                        <asp:TextBox ID="txtSobrenome" runat="server" class="form-control form-control-sm" ReadOnly="true"></asp:TextBox>
                        </div>
                    </div>
                </div>

                <!-- CPF -->
                <div class="row">
                    <div class="col">
                        <div class="form-group">
                        <label class="form-label form-label-sm" for="formCPF">CPF</label>
                        <asp:TextBox ID="txtCPF" runat="server" class="form-control form-control-sm" ReadOnly="true"></asp:TextBox>
                        </div>
                    </div>
                </div>

                <!-- Email -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formEmail">E-mail</label>
                <asp:TextBox ID="txtEmail" runat="server" class="form-control form-control-sm" ReadOnly="true"></asp:TextBox>
                </div>

                <!-- Sexo/ Data Nasc -->
                <div class="row">
                    <div class="col">
                        <div class="form-group">
                        <label class="form-label form-label-sm" for="formSexo">Sexo</label>
                        <asp:TextBox ID="txtSexo" runat="server" class="form-control form-control-sm" ReadOnly="true"></asp:TextBox>
                        </div>
                    </div>
                    <div class="col">
                        <div class="form-group">
                        <label class="form-label form-label-sm" for="formDtNasc">Dt.Nascimento</label>
                        <asp:TextBox ID="txtDtNasc" runat="server" class="form-control form-control-sm" ReadOnly="true"></asp:TextBox>
                        </div>
                    </div>
                </div>
                
                <!-- Função -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formDepartamento">Departamento</label>
                <asp:TextBox ID="txtDepartamento" runat="server" class="form-control form-control-sm" ></asp:TextBox>
                </div>

                <!-- Cargo -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formCargo">Cargo</label>
                <asp:TextBox ID="txtCargo" runat="server" class="form-control form-control-sm" ></asp:TextBox>
                </div>

                <!-- Empresa -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formEmpresa">Empresa</label>
                <asp:TextBox ID="txtEmpresa" runat="server" class="form-control form-control-sm" ReadOnly="true"></asp:TextBox>
                <!-- <select class="custom-select" id="mselEmpresas" runat="server" disabled="disabled"> </select> -->
                </div>

                <!-- Aprovado -->
                <div class="row">
                    <div class="col">
                        <div class="form-group">
                            <label class="form-label form-label-sm" for="formAprovado">Aprovado?</label>
                            <div class="form-control form-control-sm">
                                <label class="custom-radio">Sim
                                  <asp:RadioButton ID="rbtAprovado1" runat="server" GroupName="Aprovado" Checked="true" />
                                  <span class="checkmark"></span>
                                </label>
                                <label class="custom-radio">Não
                                  <asp:RadioButton ID="rbtAprovado2" runat="server" GroupName="Aprovado"/>
                                  <span class="checkmark"></span>
                                </label>
                            </div>
                        </div>
                    </div>
                </div>

            </div>
            <div class="modal-footer">
                <asp:Button ID="btnSave" runat="server" Text="Salvar" class="btn btn-primary" OnClick="btnSave_Click" />
                <asp:Button ID="btnClose" runat="server" Text="Fechar" class="btn btn-secondary" data-dismiss="modal"/>
            </div>
        </div>
      
    </div>
</div>

<!-- Script call modal-->
<script type="text/javascript">
    function ShowModalClient() {
        $("#modalClient").modal({ show: true });
    };
</script>
