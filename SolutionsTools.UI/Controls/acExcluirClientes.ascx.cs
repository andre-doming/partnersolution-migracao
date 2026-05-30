using SolutionsTools.DAL;
using SolutionsTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SolutionsTools.UI.Controls
{
    public partial class acExcluirClientes : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
            }
        }
        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            var page = (BaseWebUi)Parent.Page;

            if (page._isRefresh)
                return;

            try
            {
                var cliPage = (Clientes)Parent.Page;

                if (string.IsNullOrEmpty(hdnId.Value))
                {
                    cliPage.DeleteSelectedClients();
                }
                else
                {
                    var id = Guid.Parse(hdnId.Value);

                    if (UserDataAccess.IsAdmin)
                    {
                        cliPage.DeleteClient(id);
                    }
                    else
                    {
                        cliPage.UpdateClient(id);
                    }

                    Tools.ShowMessage(this.Page, Page.GetType(), "Cliente excluído!", Tools.MessageType.Info);

                    cliPage.FindClients("NewFind");
                }
            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Error);
            }
        }
        public void SetIdClient(string id)
        {
            hdnId.Value = id;

            SetTitleAndMessage();
        }
        private void SetTitleAndMessage()
        {
            var info1 = string.Empty;
            var info2 = string.Empty;
            var info3 = string.Empty;

            if (UserDataAccess.IsAdmin)
            {
                info1 = "definitiva";
            }
            if (string.IsNullOrEmpty(hdnId.Value))
            {
                info2 = "(s)";
                info3 = "selecionado(s)";
            }

            //lblTitle.Text = string.Format("Excluir cliente {0}", info1);
            lblMessage.Text = string.Format("Confirma a exclusão {1} do{0} cliente{0} {2}?", info2, info1, info3);
        }
    }
}