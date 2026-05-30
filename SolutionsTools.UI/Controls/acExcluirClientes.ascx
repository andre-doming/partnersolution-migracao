<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="acExcluirClientes.ascx.cs" Inherits="SolutionsTools.UI.Controls.acExcluirClientes" %>

<asp:HiddenField ID="hdnId" runat="server" />

<div class="modal fade" id="modalDelete">
	<div class="modal-dialog">
		<div class="modal-content">
			<div class="modal-header">	
				<h4 class="modal-title"><asp:Label runat="server" ID="lblTitle" Text="Excluir cliente"></asp:Label></h4>
				<button type="button" class="close" data-dismiss="modal">&times;</button>
			</div>
			<div class="modal-body">
				<span><asp:Label runat="server" ID="lblMessage" Text="Confirma a exclusão do cliente?"></asp:Label></span>
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
    function ShowModalDeleteClient() {
        $("#modalDelete").modal({ show: true });
	};
</script>