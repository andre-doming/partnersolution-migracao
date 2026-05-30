using SolutionsTools.DAL;
using SolutionsTools.Services;
using System;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace SolutionsTools.UI
{
    public partial class acEmpresas : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                VerifyUpdateControls();
            }
        }
        private void VerifyUpdateControls()
        {
            var allowUpd = UserDataAccess.IsAdmin ? true : UserDataAccess.HasFunction("funcEmpresasUpd");
            txtNomeFantasia.ReadOnly = !allowUpd;
            txtRazaoSocial.ReadOnly = !allowUpd;
            txtGerenteResponsavel.ReadOnly = !allowUpd;
            txtCnpj.ReadOnly = !allowUpd;
            rbtAtivo1.Enabled = allowUpd;
            rbtAtivo2.Enabled = allowUpd;
            btnSave.Visible = allowUpd;
        }
        protected void btnSave_Click(object sender, EventArgs e)
        {
            var page = (BaseWebUi)Parent.Page;

            if (page._isRefresh)
                return;

            try
            {
                var empresa = new Empresa();

                var id = Convert.ToInt32(hdnId.Value);

                if (id > 0)
                {
                    var dal = new EmpresaDAL();
                    empresa = dal.GetById(id);
                }

                SaveCompany(empresa);

                var msg = id.Equals(0) ? "inserida" : "atualizada";

                var empPage = (Empresas)Parent.Page;

                Tools.ShowMessage(this.Page, Page.GetType(), string.Format("Empresa {0}!", msg), Tools.MessageType.Info);

                empPage.FindCompanies();
            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Error);
            }
        }
        public void GetDataClient(int id, object sender)
        {
            try
            {
                if (id != 0)
                {
                    var dal = new EmpresaDAL();

                    var empresa = dal.GetById(id);

                    FillCompanyFields(empresa);
                }
                else
                {
                    ClearCompanyFields();
                }
            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Error);
            }
        }
        private void ClearCompanyFields()
        {
            hdnId.Value = "0"; 
            txtNomeFantasia.Text = string.Empty;
            txtRazaoSocial.Text = string.Empty;
            txtGerenteResponsavel.Text = string.Empty;
            txtCnpj.Text = string.Empty;
            rbtAtivo1.Checked = true;
            rbtAtivo2.Checked = false;
        }
        private void FillCompanyFields(Empresa empresa)
        {
            hdnId.Value = empresa.Id.ToString();
            txtNomeFantasia.Text = empresa.Nome_Fantasia;
            txtRazaoSocial.Text = empresa.Razao_Social;
            txtGerenteResponsavel.Text = empresa.Gerente_Responsavel;
            txtCnpj.Text = empresa.Cnpj;
            rbtAtivo1.Checked = empresa.Ativo.Equals("S");
            rbtAtivo2.Checked = empresa.Ativo.Equals("N");
        }
        private void SaveCompany(Empresa empresa)
        {
            empresa.Id = Convert.ToInt32(hdnId.Value);
            empresa.Nome_Fantasia = txtNomeFantasia.Text;
            empresa.Razao_Social = txtRazaoSocial.Text;
            empresa.Gerente_Responsavel = txtGerenteResponsavel.Text;
            empresa.Cnpj = Regex.Replace(txtCnpj.Text, "[^0-9]+", "");
            empresa.Ativo = rbtAtivo1.Checked ? "S" : "N";

            var dal = new EmpresaDAL();
            var ps = new PartnerServices();

            if (empresa.Id > 0)
            {
                var retorno = ps.UpdPartner(empresa);

                if (!retorno.DocumentId.Equals(new Guid()))
                {
                    dal.Update(empresa);
                }
            }
            else
            {
                var empRet = dal.GetByCnpj(empresa.Cnpj);

                if (empRet.Id == 0)
                {
                    var retorno = ps.AddPartner(empresa);

                    if (retorno.DocumentId != null)
                    {
                        empresa.Id_Parceiro = retorno.DocumentId;
                        dal.Insert(empresa);
                    }
                }
                else
                {
                    throw new Exception("Não foi possível inserir, pois a empresa já existe na base.");
                }
            }
        }
    }
}