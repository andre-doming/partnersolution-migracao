<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="acExcluirEmpresas.ascx.cs" Inherits="SolutionsTools.UI.Controls.acExcluirEmpresas" %>

<div class="modal fade" id="modalDelete">
	<div class="modal-dialog">
		<div class="modal-content">
			<div class="modal-header">	
				<h4 class="modal-title">Excluir Empresa</h4>
				<button type="button" class="close" data-dismiss="modal">&times;</button>
			</div>
			<div class="modal-body">
				<asp:HiddenField ID="hdnId" runat="server" />

				<span>Confirma a exclusão da empresa?</span>
			</div>
			<div class="modal-footer">
                <asp:Button ID="btnCancel" runat="server" Text="Cancelar" class="btn btn-default" data-dismiss="modal"/>
                <asp:Button ID="btnConfirm" runat="server" Text="Confirmar" class="btn btn-success" OnClick="btnConfirm_Click" />
			</div>
		</div>
	</div>
</div>

<!-- Script call modal-->
<script type="text/javascript">
    function ShowModalDeleteCompany() {
        $("#modalDelete").modal({ show: true });
    };
</script>