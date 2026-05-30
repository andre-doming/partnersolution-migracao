using System.Web;

namespace SolutionsTools.UI
{
    public static class UserDataAccess
    {
        public static int IdUsuario
        {
            get => HttpContext.Current.Session["idUsuario"] != null ? (int)HttpContext.Current.Session["idUsuario"] : 0;
            set => HttpContext.Current.Session["idUsuario"] = value;
        }
        public static string NomeUsuario 
        { 
            get => HttpContext.Current.Session["nomeUsuario"] != null ? (string)HttpContext.Current.Session["nomeUsuario"] : string.Empty; 
            set => HttpContext.Current.Session["nomeUsuario"] = value; 
        }
        public static string[] Funcoes 
        { 
            get => HttpContext.Current.Session["funcoes"] != null ? (string[])HttpContext.Current.Session["funcoes"] : new string[0];
            set => HttpContext.Current.Session["funcoes"] = value; 
        }
        public static int[] Empresas 
        { 
            get => HttpContext.Current.Session["empresas"] != null ? (int[])HttpContext.Current.Session["empresas"] : new int[0];
            set => HttpContext.Current.Session["empresas"] = value; 
        }
        public static bool IsAdmin 
        { 
            get => HttpContext.Current.Session["isAdmin"] != null ? (bool)HttpContext.Current.Session["isAdmin"] : false;
            set => HttpContext.Current.Session["isAdmin"] = value; 
        }

        public static void ClearSession() 
        {
            HttpContext.Current.Session.Clear();
        }

        public static bool HasFunction(string function)
        {
            for (int i = 0; i < Funcoes.Length; i++)
            {
                if (Funcoes[i].Equals(function))
                    return true;
            }

            return false;
        }
    }
}