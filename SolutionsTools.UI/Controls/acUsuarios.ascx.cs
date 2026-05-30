using Aon.Utilities.UserInterface;
using SolutionsTools.BLL;
using SolutionsTools.DAL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SolutionsTools.UI
{
    public partial class acUsuarios : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                Utils.LoadCompanies(mselEmpresas, false);
                Utils.LoadFunctions(mselFuncoes, false);
                VerifyUpdateControls();
            }
        }
        private void VerifyUpdateControls()
        {
            var allowUpd = UserDataAccess.IsAdmin ? true : UserDataAccess.HasFunction("funcUsuariosUpd");
            txtNome.ReadOnly = !allowUpd;
            txtEmail.ReadOnly = !allowUpd;
            rbtAdmin1.Enabled = allowUpd;
            rbtAdmin2.Enabled = allowUpd;
            //rbtToken1.Enabled = allowUpd;
            //rbtToken2.Enabled = allowUpd;
            rbtAtivo1.Enabled = allowUpd;
            rbtAtivo2.Enabled = allowUpd;
            mselEmpresas.Disabled = !allowUpd;
            mselFuncoes.Disabled = !allowUpd;
            btnSave.Visible = allowUpd;
        }
        protected void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var usuario = new Usuario();

                var id = Convert.ToInt32(hdnId.Value);

                if (id > 0)
                {
                    var dal = new UsuarioDAL();
                    usuario = dal.GetById(id);
                }

                SaveUser(usuario);

                var msg = id.Equals(0) ? "inserido" : "atualizado";

                Tools.ShowMessage(this.Page, Page.GetType(), string.Format("Usuario {0}!", msg), Tools.MessageType.Info);

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
                    var dal = new UsuarioDAL();

                    var usuario = dal.GetById(id);

                    FillUserFields(usuario);
                }
                else
                { 
                }
            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Error, sender);
            }
        }
        private void FillUserFields(Usuario usuario)
        {
            hdnId.Value = usuario.Id.ToString();
            txtNome.Text = usuario.Nome;
            txtLogin.Text = usuario.Login;
            txtEmail.Text = usuario.Email;
            rbtAdmin1.Checked = usuario.Admin.Equals("S");
            rbtAdmin2.Checked = usuario.Admin.Equals("N");
            rbtToken1.Checked = usuario.Acesso_Token.Equals("S");
            rbtToken2.Checked = usuario.Acesso_Token.Equals("N");
            rbtAtivo1.Checked = usuario.Ativo.Equals("S");
            rbtAtivo2.Checked = usuario.Ativo.Equals("N");

            FillUserCompanyField(usuario.Id);
            FillUserFunctionField(usuario.Id);
        }
        private void FillUserCompanyField(int id) 
        {
            var dal = new EmpresaUsuarioDAL();

            var empresas = dal.GetByUser(id);

            foreach (ListItem item in mselEmpresas.Items)
            {
                var lista = empresas.Where(e => e.Id_Empresa.Equals(Convert.ToInt32(item.Value)));

                if (lista.Count() > 0)
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }
        private void FillUserFunctionField(int id)
        {
            var dal = new FuncaoUsuarioDAL();

            var funcoes = dal.GetByUser(id);

            foreach (ListItem item in mselFuncoes.Items)
            {
                var lista = funcoes.Where(e => e.Id_Funcao.Equals(Convert.ToInt32(item.Value)));

                if (lista.Count() > 0)
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }
        private void SaveUser(Usuario usuario)
        {
            usuario.Id = Convert.ToInt32(hdnId.Value);
            usuario.Nome = txtNome.Text;
            usuario.Login = txtLogin.Text;
            usuario.Email = txtEmail.Text;
            usuario.Admin = rbtAdmin1.Checked ? "S" : "N";
            usuario.Acesso_Token = rbtToken1.Checked ? "S" : "N";
            usuario.Ativo = rbtAtivo1.Checked ? "S" : "N";
            usuario.Id_Usuario_Cadastro = UserDataAccess.IdUsuario;
            if (usuario.Primeiro_Acesso == null)
            {
                usuario.Primeiro_Acesso = "S";
            }
            else
            {
                usuario.Primeiro_Acesso = usuario.Primeiro_Acesso.Equals("S") ? "S" : usuario.Id.Equals(0) ? "S" : "N";
            }

            var password = string.Empty;

            if (string.IsNullOrEmpty(usuario.Login)) 
            {
                var pg = new PasswordGenerator(15, 16, 2, 2, 2, 2);
                password = pg.Generate();
                var md5 = new CryptographyMD5();
                var tokenLogin = usuario.Acesso_Token == "S" ? "token_" : string.Empty;
                usuario.Login = string.Format("{0}{1}", tokenLogin,LoginGenerator.Generate(usuario.Nome));
                usuario.Senha = md5.ReturnMD5(password);
            }

            var dal = new UsuarioDAL();

            if (usuario.Id > 0)
            {
                dal.Update(usuario);
            }
            else
            {
                usuario.Id = Convert.ToInt32(dal.Insert(usuario));
                PrepareMail(usuario.Nome, usuario.Login, usuario.Email, password);
            }

            SaveUserCompany(usuario.Id);
            SaveUserFunction(usuario.Id);
        }
        private void PrepareMail(string nome, string login, string email, string password)
        {
            var subject = "Confirmação de cadastro";

            var body = GetMailBody(nome, login, password);

            SendMail.Send(login, email, subject, body);
        }
        private void SaveUserCompany(int id)
        {
            var dal = new EmpresaUsuarioDAL();

            dal.DeleteByUser(id);

            foreach (ListItem item in mselEmpresas.Items)
            {
                if (item.Selected)
                {
                    var empresaUsuario = new EmpresaUsuario();
                    empresaUsuario.Id_Empresa = Convert.ToInt32(item.Value);
                    empresaUsuario.Id_Usuario = id;
                    dal.Insert(empresaUsuario);
                } 
            }
        }
        private void SaveUserFunction(int id)
        {
            var dal = new FuncaoUsuarioDAL();

            dal.DeleteByUser(id);

            foreach (ListItem item in mselFuncoes.Items)
            {
                if (item.Selected)
                {
                    var funcaoUsuario = new FuncaoUsuario();
                    funcaoUsuario.Id_Funcao = Convert.ToInt32(item.Value);
                    funcaoUsuario.Id_Usuario = id;
                    dal.Insert(funcaoUsuario);
                }
            }
        }
        private static string GetMailBody(string name, string login, string password)
        {
            var body = Utils.ReadFile(@"\Template\Email_Novo_Acesso.html");

            var firstName = name.Split(' ')[0];

            body = body.Replace("_name", firstName);

            body = body.Replace("_login", login);

            body = body.Replace("_password", password);

            return body;
        }
    }
}