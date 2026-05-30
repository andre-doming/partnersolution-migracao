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
    public partial class acExcluirEmpresas : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            var page = (BaseWebUi)Parent.Page;

            if (page._isRefresh)
                return;

            try
            {
                var id = Convert.ToInt32(hdnId.Value);

                UpdateClient(id);

                var empPage = (Empresas)Parent.Page;

                Tools.ShowMessage(this.Page, Page.GetType(), "Empresa excluída!", Tools.MessageType.Info);

                empPage.FindCompanies();

            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Error);
            }
        }
        public void SetIdClient(string id) 
        {
            hdnId.Value = id;
        }
        private void UpdateClient(int id)
        {
            var ps = new PartnerServices();

            var dal = new EmpresaDAL();
            var empresa = dal.GetById(id);
            empresa.Ativo = "N";

            var ret = ps.UpdPartner(empresa);
            if (!ret.DocumentId.Equals(new Guid()))
            {
                dal.Update(empresa);
            }

        }
    }
}