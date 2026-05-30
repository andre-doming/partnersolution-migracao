using SolutionsTools.DAL;
using System;
using System.Text.RegularExpressions;

namespace SolutionsTools.WebApi
{
    public static class Utils
    {
        public static void Log(string login, EntityType tipoEntidade, string guid_cliente, string movimentacao) 
        {
            var dal = new LogDAL();

            var entidade = ((EntityType)tipoEntidade).ToString();

            dal.InsertByProc(login, entidade, guid_cliente, movimentacao);
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
    }
}