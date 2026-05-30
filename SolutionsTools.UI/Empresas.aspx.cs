using Aon.Utilities.UserInterface;
using SolutionsTools.DAL;
using SolutionsTools.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SolutionsTools.UI
{
    public partial class Empresas : BaseWebUi
    {
        public int idEmpresa { get => (int)Session["IdEmpresa"]; set => Session["IdEmpresa"] = value; }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                FindCompanies();
                VerifyInsertControls();
            }
        }
        protected void btnListar_Click(object sender, EventArgs e)
        {
            FindCompanies();
        }
        private void VerifyInsertControls()
        {
            var allowIns = UserDataAccess.IsAdmin ? true : UserDataAccess.HasFunction("funcEmpresasIns");
            btnAdicionar.Visible = allowIns;
        }
        protected void grvEmpresas_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            grvEmpresas.PageIndex = e.NewPageIndex;
            FindCompanies();
        }
        protected void grvEmpresas_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Page")
                return;

            int rowIndex = Convert.ToInt32(e.CommandArgument) - (grvEmpresas.PageIndex * grvEmpresas.PageSize);

            idEmpresa = Convert.ToInt32(grvEmpresas.DataKeys[rowIndex].Value);

            if (e.CommandName == "Upd")
            {
                acModalCompany.GetDataClient(idEmpresa, btnListar);

                ShowModalCompany(this.Page, Page.GetType());
            }
            if (e.CommandName == "Del")
            {
                acModalDeleteCompany.SetIdClient(idEmpresa.ToString());

                ShowModalDeleteCompany(this.Page, Page.GetType());

            }
        }
        protected void btnAdicionar_Click(object sender, EventArgs e)
        {
            acModalCompany.GetDataClient(0, btnListar);

            ShowModalCompany(this.Page, Page.GetType());
        }
        public void FindCompanies()
        {
            var dal = new EmpresaDAL();

            var empresas = new List<Empresa>();

            if (!string.IsNullOrEmpty(txtProcurarPor.Text))
            {
                empresas = dal.GetByCompanyAndCustom(mselProcurarPor.Value, txtProcurarPor.Text);
            }
            else
            {
                empresas = dal.GetCompanies().ToList();
            }

            empresas = empresas.OrderBy(e => e.Nome_Fantasia).ToList();

            if (!UserDataAccess.IsAdmin)
            {
                empresas = (from e in empresas
                            where UserDataAccess.Empresas.Contains(e.Id)
                            select e).ToList();
            }

            RestartDivPosition(this.Page, Page.GetType());

            grvEmpresas.DataSource = empresas;
            grvEmpresas.DataBind();
        }
        private void ShowModalCompany(Page page, Type type)
        {
            var msg = string.Format("javascript: ShowModalCompany();");

            ScriptManager.RegisterStartupScript(page, type, "script", msg, true);
        }
        private void ShowModalDeleteCompany(Page page, Type type)
        {
            var msg = string.Format("javascript: ShowModalDeleteCompany();");

            ScriptManager.RegisterStartupScript(page, type, "script", msg, true);
        }
        private void DeleteCompany(int id)
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
        protected void grvEmpresas_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            var allowDel = UserDataAccess.IsAdmin ? true : UserDataAccess.HasFunction("funcEmpresasDel");

            if (e.Row.RowType == DataControlRowType.Header || e.Row.RowType == DataControlRowType.DataRow)
                e.Row.Cells[1].Visible = allowDel;
        }
        private void RestartDivPosition(Page page, Type type)
        {

            var msg = string.Format("javascript: RestartDivPosition();");

            ScriptManager.RegisterStartupScript(page, type, "script", msg, true);

        }
    }
}