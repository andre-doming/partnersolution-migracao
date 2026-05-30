using SolutionsTools.DAL;
using System;
using System.IO;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SolutionsTools.UI
{
    public partial class Site : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (UserDataAccess.IdUsuario.Equals(0))
            {
                Response.Redirect("Login.aspx");
            }
            else
            {
                if (!IsAuthorizationOnPage(UserDataAccess.IsAdmin))
                {
                    if (!Page.AppRelativeVirtualPath.Contains("Home.aspx"))
                    {
                        Response.Redirect("Home.aspx");
                    }
                }
                SetMenuFunctions(UserDataAccess.IsAdmin);
            }
        }
        private bool IsAuthorizationOnPage(bool isAdmin) 
        {
            if (isAdmin)
            {
                return true;
            }

            var funcao = (HiddenField)Page.Master.FindControl("MainContent").FindControl("Funcao");

            if (funcao.Value.Equals("funcHome"))
            {
                return true;
            }
            else
            {
                return UserDataAccess.HasFunction(funcao.Value);
            }
        }
        private void SetMenuFunctions(bool isAdmin)
        {
            if (isAdmin)
            {
                funcImpCsv.Visible = true;
                funcClientes.Visible = true;
                funcEmpresas.Visible = true;
                funcUsuarios.Visible = true;
                return;
            }
            
            var funcoes = UserDataAccess.Funcoes;

            for (int i = 0; i < funcoes.Length; i++)
            {
                var li = (HtmlGenericControl)FindControl(funcoes[i]);

                if (li != null)
                {
                    li.Visible = true;
                }
            }
        }
        protected void btnLogoff_Click(object sender, EventArgs e)
        {
            UserDataAccess.ClearSession();
            Response.Redirect("Login.aspx");
        }
    }
}