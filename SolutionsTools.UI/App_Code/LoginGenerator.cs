using SolutionsTools.BLL;
using System.Text.RegularExpressions;

namespace SolutionsTools.UI
{
    public static class LoginGenerator
    {
        public static string Generate(string completeName)
        {
            var login = completeName;
            var result = login.LastIndexOf(' ');
            login = login[0].ToString().ToLower() + login.Substring(result + 1).ToLower();

            login = Regex.Replace(login, "[^0-9a-zA-Z]+", "");

            var bll = new UsuarioBLL();

            bool existeusu = bll.ExisteLogin(login);

            if (existeusu != false)
            {
                int i = 1;
                while (existeusu != false)
                {
                    existeusu = bll.ExisteLogin(login + i);
                    i++;
                }
                login = login + (i - 1);
            }

            return login;
        }
    }
}