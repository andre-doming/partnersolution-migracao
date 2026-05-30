using SolutionsTools.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SolutionsTools.UI
{
    public static class PasswordServices
    {
        public static void Recovery(string login)
        {
            if (login.Contains("@")) 
            {
                throw new Exception("O usuário informado não é válido pois foi informado um email");
            }

            var dal = new UsuarioDAL();

            var pg = new PasswordGenerator(15, 20, 2, 2, 2, 2);

            var usuario = dal.GetByLogin(login);

            if (usuario == null)
            {
                throw new Exception("Usuário não encontrado");
            }

            var password = pg.Generate();
            var md5 = new CryptographyMD5();
            usuario.Senha = md5.ReturnMD5(password);
            usuario.Primeiro_Acesso = "S";

            dal.Update(usuario);

            var subject = "Redefinição de senha";

            var body = GetMailBody(usuario.Nome, usuario.Login, password);

            SendMail.Send(usuario.Login, usuario.Email, subject, body);

        }
        private static string GetMailBody(string name, string login, string password)
        {
            var body = Utils.ReadFile(@"\Template\Email_Redefinicao.html");

            var firstName = name.Split(' ')[0];

            body = body.Replace("_name", firstName);

            body = body.Replace("_login", login);

            body = body.Replace("_password", password);

            body = body.Replace("_data", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

            return body;
        }

    }
}