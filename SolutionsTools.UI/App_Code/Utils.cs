using Aon.Utilities.UserInterface;
using SolutionsTools.DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SolutionsTools.UI
{
    public static class Utils
    {
        public static void LoadCompanies(DropDownList ddl)
        {
            var dal = new EmpresaDAL();

            var empresas = dal.GetCompanies();

            if (!UserDataAccess.IsAdmin)
            {
                empresas = from e in empresas
                           where UserDataAccess.Empresas.Contains(e.Id)
                           select e;
            }

            empresas = empresas.OrderBy(e => e.Nome_Fantasia).ToList();

            ddl.DataValueField = "Id";
            ddl.DataTextField = "Nome";

            if (empresas.Count() > 1)
            {
                ddl.BindData(empresas, new ListItem("Todos", "0"));
            }
            else
            {
                ddl.BindData(empresas);
            }
        }
        public static void LoadCompanies(HtmlSelect sel, bool includeAll, bool useGuid = false)
        {
            var dal = new EmpresaDAL();

            var empresas = dal.GetCompanies();

            if (!UserDataAccess.IsAdmin)
            {
                empresas = from e in empresas
                           where UserDataAccess.Empresas.Contains(e.Id)
                           select e;
            }

            empresas = empresas.OrderBy(e => e.Nome_Fantasia).ToList();

            if (empresas.Count() > 1 && includeAll)
            {
                var list = new ListItem();
                list.Value = "0";
                list.Text = "Todos";
                sel.Items.Add(list);
            }
            
            foreach (var empresa in empresas)
            {
                var list = new ListItem();
                list.Value = useGuid ? empresa.Id_Parceiro.ToString() : empresa.Id.ToString();
                list.Text = empresa.Nome_Fantasia;
                list.Selected = false;
                sel.Items.Add(list);
            }

        }
        public static void LoadFunctions(HtmlSelect sel, bool includeAll)
        {
            var dal = new FuncaoDAL();

            var funcoes = dal.GetFunctions();

            if (!UserDataAccess.IsAdmin)
            {
                funcoes = from f in funcoes
                          where UserDataAccess.Funcoes.Contains(f.Cod_Funcao)
                          select f;
            }

            funcoes = funcoes.OrderBy(e => e.Nome_Funcao).ToList();

            if (funcoes.Count() > 1 && includeAll)
            {
                var list = new ListItem();
                list.Value = "0";
                list.Text = "Todos";
                sel.Items.Add(list);
            }

            foreach (var funcao in funcoes)
            {
                var list = new ListItem();
                list.Value = funcao.Id.ToString();
                list.Text = funcao.Nome_Funcao;
                list.Selected = false;
                sel.Items.Add(list);
            }
        }
        public static string ReadFile(string file) 
        {
            StreamReader sr = File.OpenText(HttpContext.Current.Server.MapPath(file));
            
            string contents = sr.ReadToEnd();

            return contents;
        }
        public static void Log(EntityType tipoEntidade, Guid guid_cliente, string acao) 
        {
            var dal = new LogDAL();
            
            var log = new Log{ 
                Dt_log = DateTime.Now,
                Id_Usuario = UserDataAccess.IdUsuario,
                Entidade = ((EntityType)tipoEntidade).ToString(),
                Id_Cliente = guid_cliente,
                Acao = acao
            };

            dal.Insert(log);
        }
        public static string FormatCPF(string cpf)
        {
            string response = Regex.Replace(cpf.Trim(), "[^0-9]+", "");

            if (response.Length.Equals(0))
            {
                return string.Empty;
            }
                

            if (response.Length > 11)
            {
                response = response.Substring(response.Length - 11, 11);
            }

            response = Convert.ToUInt64(response).ToString(@"000\.000\.000\-00");

            return response;
        }
        public static string FormatCNPJ(string cnpj)
        {
            string response = Regex.Replace(cnpj.Trim(), "[^0-9]+", "");

            if (response.Length > 14)
            {
                response = response.Substring(response.Length - 14, 14);
            }

            response = Convert.ToUInt64(response).ToString(@"000\.000\.000\-00");

            return response;
        }
        public static bool IsCpf (string cpf)
        {
            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            cpf = cpf.Trim().Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
                return false;

            for (int j = 0; j < 10; j++)
                if (j.ToString().PadLeft(11, char.Parse(j.ToString())) == cpf)
                    return false;

            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cpf.EndsWith(digito);
        }
        public static bool IsCnpj(string cnpj)
        {
            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            cnpj = cnpj.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14)
                return false;

            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

            int resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cnpj.EndsWith(digito);
        }
        public static bool IsEmail(string email)
        {
            var regex = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";
            bool isValid = Regex.IsMatch(email, regex, RegexOptions.IgnoreCase);
            return isValid;
        }
    }
}