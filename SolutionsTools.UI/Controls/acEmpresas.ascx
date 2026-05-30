<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="acEmpresas.ascx.cs" Inherits="SolutionsTools.UI.acEmpresas" %>

<!-- Custom style -->
<link href="../Content/custom-radio.css" rel="stylesheet" />

<!-- Modal -->
<div class="modal fade" id="modalCompany" role="dialog">
    
    <div class="modal-dialog modal-dialog-centered modal-lg"">
    
        <!-- Modal content-->
        <div class="modal-content">
            <div class="modal-header">
                <h4 class="modal-title">Dados do usuário</h4>
                <button type="button" class="close" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">

                <asp:HiddenField ID="hdnId" runat="server" Value="0"/>

                <!-- Nome Fantasia -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formNomeFantasia">Nome Fantasia</label>
                <asp:TextBox ID="txtNomeFantasia" runat="server" class="form-control form-control-sm" MaxLength="100"></asp:TextBox>
                </div>

                <!-- Razão Social -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formRazaoSocial">Razão Social</label>
                <asp:TextBox ID="txtRazaoSocial" runat="server" class="form-control form-control-sm" MaxLength="100"></asp:TextBox>
                </div>

                <!-- Gerente Responsavel -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formGerenteResponsavel">Gerente Responsável</label>
                <asp:TextBox ID="txtGerenteResponsavel" runat="server" class="form-control form-control-sm" MaxLength="50"></asp:TextBox>
                </div>

                <!-- CNPJ -->
                <div class="form-group">
                <label class="form-label form-label-sm" for="formLogin">CNPJ</label>
                <asp:TextBox ID="txtCnpj" runat="server" class="form-control form-control-sm cnpj" ReadOnly="true"></asp:TextBox>
                </div>

                <!-- Ativo -->
                <div class="row">
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

            </div>
            <div class="modal-footer">
                <asp:Button ID="btnSave" runat="server" Text="Salvar" class="btn btn-primary" OnClientClick="return validarCNPJ();" OnClick="btnSave_Click" />
                <asp:Button ID="btnClose" runat="server" Text="Fechar" class="btn btn-secondary" data-dismiss="modal"/>
            </div>
        </div>

    </div>

</div>

<!-- Script call modal-->
<script type="text/javascript">
    function ShowModalCompany() {
        $("#modalCompany").modal({ show: true });
    };

    function validarCNPJ() {
        var cnpj = $(".cnpj");

        $(".erromsg").remove();

        var erromsg = '<div class="erromsg">* <span></span></div>';

        if (!isCNPJ(cnpj.val())) {
            cnpj.after(erromsg);
            $(".erromsg span").text("O Cnpj informado é inválido");
            return false;
        }

        return true;
    }

    function isCNPJ(cnpj) {

        if (cnpj == '00000000000000')
            return true;

        cnpj = cnpj.replace(/[^\d]+/g,'');
 
        if(cnpj == '') return false;
     
        if (cnpj.length != 14)
            return false;
 
        // Elimina CNPJs invalidos conhecidos
        if (cnpj == "00000000000000" || 
            cnpj == "11111111111111" || 
            cnpj == "22222222222222" || 
            cnpj == "33333333333333" || 
            cnpj == "44444444444444" || 
            cnpj == "55555555555555" || 
            cnpj == "66666666666666" || 
            cnpj == "77777777777777" || 
            cnpj == "88888888888888" || 
            cnpj == "99999999999999")
            return false;
         
        // Valida DVs
        tamanho = cnpj.length - 2
        numeros = cnpj.substring(0,tamanho);
        digitos = cnpj.substring(tamanho);
        soma = 0;
        pos = tamanho - 7;
        for (i = tamanho; i >= 1; i--) {
          soma += numeros.charAt(tamanho - i) * pos--;
          if (pos < 2)
                pos = 9;
        }
        resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
        if (resultado != digitos.charAt(0))
            return false;
         
        tamanho = tamanho + 1;
        numeros = cnpj.substring(0,tamanho);
        soma = 0;
        pos = tamanho - 7;
        for (i = tamanho; i >= 1; i--) {
          soma += numeros.charAt(tamanho - i) * pos--;
          if (pos < 2)
                pos = 9;
        }
        resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
        if (resultado != digitos.charAt(1))
              return false;
           
        return true;
    
    }
</script>

