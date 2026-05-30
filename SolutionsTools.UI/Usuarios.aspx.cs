using SolutionsTools.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SolutionsTools.UI
{
    public partial class Usuarios : BaseWebUi
    {
        public int idUsuario { get => (int)Session["IdUsuario"]; set => Session["IdUsuario"] = value; }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                Utils.LoadCompanies(mselEmpresas, true);
                VerifyInsertControls();
            }
        }
        protected void btnListar_Click(object sender, EventArgs e)
        {
            FindUsers();
        }
        private void VerifyInsertControls()
        {
            var allowUpd = UserDataAccess.IsAdmin ? true : UserDataAccess.HasFunction("funcUsuariosIns");
            btnAdicionar.Visible = allowUpd;
        }
        protected void grvUsuarios_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            grvUsuarios.PageIndex = e.NewPageIndex;
            FindUsers();
        }
        protected void grvUsuarios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Page")
                return;

            int rowIndex = Convert.ToInt32(e.CommandArgument) - (grvUsuarios.PageIndex * grvUsuarios.PageSize);

            idUsuario = Convert.ToInt32(grvUsuarios.DataKeys[rowIndex].Values[0]);

            if (e.CommandName == "Upd")
            {
                acModalUser.GetDataClient(idUsuario, btnListar);

                ShowModal(this.Page, Page.GetType());
            }
            if (e.CommandName == "Del")
            {
                DeleteUser(idUsuario);

                Tools.ShowMessage(this.Page, Page.GetType(), "Usuário excluído!", Tools.MessageType.Info);
            }
            if (e.CommandName == "Recovery")
            {
                var login = grvUsuarios.DataKeys[rowIndex].Values[1].ToString(); 

                RecoveryPassword(login);
            }
        }
        protected void btnAdicionar_Click(object sender, EventArgs e)
        {
            acModalUser.GetDataClient(0, btnListar);

            ShowModal(this.Page, Page.GetType());
        }
        private void FindUsers()
        {
            var id_Empresa = Convert.ToInt32(mselEmpresas.Value);
            var dal = new UsuarioDAL();
            var usuarios = new List<Usuario>();

            if (!string.IsNullOrEmpty(txtProcurarPor.Text))
            {
                usuarios = dal.GetByCompanyAndCustom(id_Empresa, mselProcurarPor.Value, txtProcurarPor.Text);
            }
            else
            {
                usuarios = dal.GetByCompany(id_Empresa).ToList();
            }

            usuarios = usuarios.OrderBy(e => e.Nome).ToList();

            if (!UserDataAccess.IsAdmin)
            {
                usuarios = usuarios.Where(u => u.Id_Usuario_Cadastro.Equals(UserDataAccess.IdUsuario)).ToList();
            }

            RestartDivPosition(this.Page, Page.GetType());

            grvUsuarios.DataSource = usuarios;
            grvUsuarios.DataBind();
        }
        private void ShowModal(Page page, Type type)
        {
            var msg = string.Format("javascript: ShowModalUser();");

            ScriptManager.RegisterStartupScript(page, type, "script", msg, true);
        }
        private void RestartDivPosition(Page page, Type type)
        {
            var msg = string.Format("javascript: RestartDivPosition();");

            ScriptManager.RegisterStartupScript(page, type, "script", msg, true);
        }
        private void RecoveryPassword(string login) 
        {
            try
            {
                PasswordServices.Recovery(login);

                Tools.ShowMessage(this.Page, Page.GetType(), "Senha redefinida e usuário notificado!", Tools.MessageType.Info);
            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Error);
            }

        }
        private void DeleteUser(int id)
        {
            var dal = new UsuarioDAL();
            var usuario = dal.GetById(id);
            usuario.Ativo = "N";
            dal.Update(usuario);
        }
        protected void grvUsuarios_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            var allowDel = UserDataAccess.IsAdmin ? true : UserDataAccess.HasFunction("funcUsuariosDel");

            if (e.Row.RowType == DataControlRowType.Header || e.Row.RowType == DataControlRowType.DataRow)
                e.Row.Cells[1].Visible = allowDel;
        }
    }
}