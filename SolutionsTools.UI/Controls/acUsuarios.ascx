<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="acUsuarios.ascx.cs" Inherits="SolutionsTools.UI.acUsuarios" %>

<!-- Custom style -->
<link href="../Content/custom-radio.css" rel="stylesheet" />

<!-- Modal -->
<div class="modal fade" id="modalUser" role="dialog">
    
    <div class="modal-dialog modal-dialog-centered modal-lg"">
    
        <!-- Modal content-->
        <div class="modal-content">
            <div class="modal-header">
                <h4 class="modal-title">Dados do usuário</h4>
                <button type="button" class="close" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">

                <asp:HiddenField ID="hdnId" runat="server" Value="0"/>

                <!-- Nome -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formNome">Nome</label>
                <asp:TextBox ID="txtNome" runat="server" class="form-control form-control-sm"></asp:TextBox>
                </div>

                <!-- Login -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formLogin">Login</label>
                <asp:TextBox ID="txtLogin" runat="server" class="form-control form-control-sm" ReadOnly="true"></asp:TextBox>
                </div>

                <!-- Email -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formEmail">E-mail</label>
                <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" class="form-control form-control-sm"></asp:TextBox>
                </div>

                <!-- Token/Admin/Ativo -->
                <div class="row">
                    <div class="col"><!-- Token -->
                        <div class="form-group">
                            <label class="form-label form-label-sm" for="formToken">Token?</label>
                            <div class="form-control form-control-sm">
                                <label class="custom-radio">Sim
                                  <asp:RadioButton ID="rbtToken1" runat="server" GroupName="Token" Enabled="false" />
                                  <span class="checkmark"></span>
                                </label>
                                <label class="custom-radio">Não
                                  <asp:RadioButton ID="rbtToken2" runat="server" GroupName="Token" Checked="true" Enabled="false" />
                                  <span class="checkmark"></span>
                                </label>
                            </div>
                        </div>
                    </div>
                    <div class="col"><!-- Admin -->
                        <div class="form-group">
                            <label class="form-label form-label-sm" for="formAdmin">Admin?</label>
                            <div class="form-control form-control-sm">
                                <label class="custom-radio">Sim
                                  <asp:RadioButton ID="rbtAdmin1" runat="server" GroupName="Admin"/>
                                  <span class="checkmark"></span>
                                </label>
                                <label class="custom-radio">Não
                                  <asp:RadioButton ID="rbtAdmin2" runat="server" GroupName="Admin" Checked="true" />
                                  <span class="checkmark"></span>
                                </label>
                            </div>
                        </div>
                    </div>
                    <div class="col"><!-- Ativo -->
                        <div class="form-group">
                            <label class="form-label form-label-sm" for="formAtivo">Ativo?</label>
                            <div class="form-control form-control-sm">
                                <label class="custom-radio">Sim
                                  <asp:RadioButton ID="rbtAtivo1" runat="server" GroupName="Ativo" Checked="true" />
                                  <span class="checkmark"></span>
                                </label>
                                <label class="custom-radio">Não
                                  <asp:RadioButton ID="rbtAtivo2" runat="server" GroupName="Ativo"/>
                                  <span class="checkmark"></span>
                                </label>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Empresas -->
                <div class="form-group">
                <label for="mselEmpresas">Empresas relacionadas</label>
                <select multiple="true" class="form-control form-control-sm" id="mselEmpresas" runat="server" style="height: 120px"/>
                </div>

                <!-- Funcoes -->
                <div class="form-group">
                <label for="mselFuncoes">Funções relacionadas</label>
                <select multiple="true" class="form-control form-control-sm" id="mselFuncoes" runat="server" style="height: 120px"/>
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
    function ShowModalUser() {
        $("#modalUser").modal({ show: true });
    };

    function GeneratePassword() {
        var pass = generatePassword(true, true, true, true, 16);

        var control = ""; <%--//$('#<%= txtSenha.ClientID %>');--%>

        control.val(pass);
    }

    $(document).ready(function () {
        $("#show_hide_password a").on('click', function (event) {
            event.preventDefault();
            if ($('#show_hide_password input').attr("type") == "text") {
                $('#show_hide_password input').attr('type', 'password');
                $('#show_hide_password i').addClass("fa-eye-slash");
                $('#show_hide_password i').removeClass("fa-eye");
            } else if ($('#show_hide_password input').attr("type") == "password") {
                $('#show_hide_password input').attr('type', 'text');
                $('#show_hide_password i').removeClass("fa-eye-slash");
                $('#show_hide_password i').addClass("fa-eye");
            }
        });
    });
</script>

<!-- Script app -->
<script src="../Scripts/App.js" type="text/javascript"></script>
