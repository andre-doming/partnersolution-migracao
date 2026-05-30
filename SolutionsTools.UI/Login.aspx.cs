using SolutionsTools.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SolutionsTools.UI
{
    public partial class Login : BaseWebUi
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            var qs = Request.QueryString;

            if (qs.Count > 0)
            {
                var user = qs["login"];
                var password = qs["password"];

                var login = UserAuthorizated(user, password);

                if (login.Message.Equals("Primeiro_Acesso"))
                {
                    inputUsername.Value = user;
                    ShowChange();
                }
            }
        }
        protected void btnLogin_Click(object sender, EventArgs e)
        {
            if (change_function.Value.Equals("S"))
            {
                ChangeFunction();
            }
            else 
            {
                LoginFunction();
            }
        }
        protected void LoginFunction()
        {
            var user = inputUsername.Value;
            var password = inputPassword.Value;

            var login = UserAuthorizated(user, password);

            if (login.Status)
            {
                Response.Redirect("Home.aspx");
            }
            else
            {
                if (login.Message.Equals("Primeiro_Acesso"))
                {
                    ShowChange();
                }
                else
                {
                    ShowAlert(login.Message);
                }
            }
        }
        protected void ChangeFunction()
        {
            if (inputPassword.Value != inputPasswordConfirm.Value)
            {
                ShowAlert("A senha não confere com a confirmação.");
            }
            else
            {
                var isValidPassword = PasswordGenerator.ValidatePassword(inputPassword.Value);

                if (!isValidPassword)
                {
                    var msg = "A senha deve ter: <br>" +
                              "-no mínimo 8 caracteres; <br>" +
                              "-no mínimo 1 letra maiúsculas;  <br>" +
                              "-no mínimo 1 letra minúscula; <br>" +
                              "-no mínimo 1 número; <br>" +
                              "-no mínimo 2 símbolos";

                    ShowAlert(msg);
                }
                else
                {
                    var dal = new UsuarioDAL();

                    var usuario = dal.GetByLogin(inputUsername.Value);

                    var md5 = new CryptographyMD5();
                    usuario.Senha = md5.ReturnMD5(inputPassword.Value);
                    usuario.Primeiro_Acesso = "N";

                    dal.Update(usuario);

                    ShowAlert("Senha alterada com exito");

                    ShowSign();
                }
            }
        }
        protected void btnRecovery_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(inputUsername.Value))
                {
                    ShowAlert("Digite o usuário antes de solicitar o recuperação da senha");
                    return;
                }

                if (inputUsername.Value.Equals("admin") || inputUsername.Value.Equals("token_admin"))
                {
                    ShowAlert("Não é possivel recuperar senha de usuário de sistema");
                    return;
                }
                    
                PasswordServices.Recovery(inputUsername.Value);

                ShowAlert("Uma nova senha foi enviada para o email cadastrado");
            }
            catch (Exception ex)
            {
                ShowAlert("Erro: " + ex.Message);
            }

        }
        private void ShowChange()
        {
            labelFunction.InnerText = "Alterar senha";
            inputUsername.Disabled = true;
            inputPassword.Focus();
            inputPasswordConfirm.Visible = true;
            btnLogin.InnerText = "Alterar";
            change_function.Value = "S";
        }
        private void ShowSign()
        {
            labelFunction.InnerText = "Login";
            inputUsername.Disabled = false;
            inputPassword.Focus();
            inputPasswordConfirm.Visible = false;
            btnLogin.InnerText = "Entrar";
            change_function.Value = "N";
        }
        private void SetSession(Usuario usuario)
        {
            UserDataAccess.IdUsuario = usuario.Id;
            UserDataAccess.NomeUsuario = usuario.Nome;
            UserDataAccess.IsAdmin = usuario.Admin.Equals("S");
            UserDataAccess.Empresas = GetCompanies(usuario.Id);
            UserDataAccess.Funcoes = GetFunctions(usuario.Id);
        }
        private int[] GetCompanies(int id)
        {
            var dal = new EmpresaUsuarioDAL();
            var empresas = dal.GetByUser(id).ToList();

            var array = new int[empresas.Count()];

            for (int i = 0; i < empresas.Count(); i++)
            {
                array[i] = empresas[i].Id_Empresa;
            }

            return array;
        }
        private string[] GetFunctions(int id)
        {
            var dal = new FuncaoUsuarioDAL();
            var funcoes = dal.GetByUser(id).ToList();

            var array = new string[funcoes.Count()];

            for (int i = 0; i < funcoes.Count(); i++)
            {
                array[i] = GetFunctionName(funcoes[i].Id_Funcao);
            }

            return array;
        }
        private string GetFunctionName(int id)
        {
            var dal = new FuncaoDAL();
            var funcao = dal.GetById(id);

            if (funcao != null)
            {
                return funcao.Cod_Funcao;
            }
            return string.Empty;
        }
        private Logon UserAuthorizated(string user, string password)
        {
            var ret = new Logon();

            try
            {
                var usuario = Signin(user);

                if (usuario == null)
                {
                    ret.Status = false;
                    ret.Message = "Usuário não encontrado";
                }
                else
                {
                    var crip = new CryptographyMD5();

                    if (crip.CompareMD5(usuario.Senha, password))
                    {
                        ret.Status = (!IsFirstAccess(usuario));
                        ret.Message = ret.Status ? "" : "Primeiro_Acesso";
                    }
                    else
                    {
                        ret.Status = false;
                        ret.Message = "Senha incorreta";
                    }
                }
            }
            catch (Exception ex)
            {
                ret.Status = false;
                ret.Message = ex.Message;
            }

            return ret;
        }
        private bool IsFirstAccess(Usuario usuario)
        {
            if (usuario.Primeiro_Acesso.Equals("N"))
            {
                SetSession(usuario);
                return false;
            }
            else
            {
                return true;
            }
        }
        private Usuario Signin(string user)
        {
            var dal = new UsuarioDAL();
            var usuario = dal.GetByLogin(user);
            return usuario;
        }
        public void ShowAlert(string message)
        {
            var msg = string.Format("javascript: showAlert('{0}');", message);

            ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "script", msg, true);
        }
        private struct Logon
        {
            public bool Status;
            public string Message;
        }
    }
}