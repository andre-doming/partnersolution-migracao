<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="acModal.ascx.cs" Inherits="SolutionsTools.UI.acModal" %>

<!-- Modal -->
<div class="modal fade" id="modalMessage" role="dialog">
    <div class="modal-dialog modal-dialog-centered">
    
        <!-- Modal content-->
        <div class="modal-content">
            <div class="modal-header">
                <h4 class="modal-title">Modal Title</h4>
                <button type="button" class="close" data-dismiss="modal">&times;</button>
            </div>
            <div class="modal-body">
                <p>Modal content..</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-success button-close" data-dismiss="modal">Fechar</button>
            </div>
        </div>
      
    </div>
</div>

<!-- Script call modal-->
<script type="text/javascript">
    function ShowMessage(pTitle, pMessage, pSender, pType) {
        $("#modalMessage").modal({ show: true });
        $('.modal-title').text(pTitle);
        $('.modal-body').text(pMessage);

        var mType = "btn btn-success";

        if (pType == 1)
            mType = "btn btn-warning";

        if (pType == 2)
            mType = "btn btn-danger";

        $(".button-close").removeClass("btn btn-success").addClass(mType);

        if (pSender != '')
        {
            __doPostBack(pSender, '');
        }
    };
</script>

