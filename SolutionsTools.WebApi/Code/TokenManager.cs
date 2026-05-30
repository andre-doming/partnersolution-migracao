using System;
using System.Configuration;
using System.Globalization;
using SolutionsTools.Services;

namespace SolutionsTools.WebApi.Models
{
    public class TokenManager
    {
        public ReturnGeneric ValidaToken(string token)
        {
            var ret = new ReturnGeneric();
            var user = string.Empty;
            bool erro = false;

            #region VERIFICAO DE TOKEN
            string passPhrase = ConfigurationManager.AppSettings["pass.key"];
            string saltValue = ConfigurationManager.AppSettings["salt.value"];

            try
            {
                string tokenParse = CriptoRijndael.Decrypt(token, passPhrase, saltValue);
                string[] vToken = tokenParse.Split('|');
                DateTime dtExpira = DateTime.Now;

                if (vToken == null || vToken.Length != 4)
                {
                    erro = true;
                    ret.ret_code = -139;
                    ret.ret_info = "Não foi possível identificar esse token como válido. Efetue Logon novamente.";
                }
                else
                {
                    if (!DateTime.TryParseExact(vToken[3], "dd/MM/yyyy HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None, out dtExpira))
                    {
                        erro = true;
                        ret.ret_code = -122;
                        ret.ret_info = String.Format("Token mal formado na data de expiração. Data Expiração: {0}", vToken[3]);
                    }

                    if (!erro)
                    {
                        DateTime dtNow = DateTime.Now;

                        if (dtNow > dtExpira)
                        {
                            erro = true;
                            ret.ret_code = -123;
                            ret.ret_info = "Token informado encontra-se expirado, efetue Logon novamente.";
                        }
                    }

                    user = vToken[0];
                }
            }
            catch (Exception ex)
            {
                erro = true;
                ret.ret_code = -124;
                ret.ret_info = String.Format("Token informado é inválido. Erro: {0}", ex.Message);
            }
            #endregion

            if (!erro)
            {
                ret.ret_code = 0;
                ret.ret_info = user;
            }

            return ret;
        }
        public ReturnGeneric Login(string usuario, string senha, string language)
        {
            var retorno = new ReturnGeneric();
            bool autenticado = false;
            string token = String.Empty;
            DateTime dtNow = DateTime.Now;

            string passPhrase = ConfigurationManager.AppSettings["pass.key"];
            string saltValue = ConfigurationManager.AppSettings["salt.value"];
            string expireToken = ConfigurationManager.AppSettings["expire.token"];

            double expire = 0.0;

            bool Ok = Double.TryParse(expireToken, out expire);
            
            if (!Ok) // numero informado não é um double válido fixa 2 minutos
                expire = 2.0;

            DateTime dtExpira = dtNow.AddMinutes(expire);

            string plainText = String.Format("{0}|{1}|{2}|{3}",
                usuario,
                language,
                dtNow.ToString("dd/MM/yyyy HH:mm:ss"),
                dtExpira.ToString("dd/MM/yyyy HH:mm:ss"));

            try
            {
                var ls = new UserServices();
                var ret = ls.Authenticate(usuario, senha);
                autenticado = ret.status;

                if (autenticado)
                {
                    try
                    {
                        token = CriptoRijndael.Encrypt(plainText, passPhrase, saltValue);

                        retorno.ret_code = 0;
                        retorno.ret_info = token;
                    }
                    catch (Exception ex)
                    {
                        retorno.ret_code = -104;
                        retorno.ret_info = String.Format("Problemas ao gerar o token de autenticação. Erro: {0}", ex.Message);
                    }
                }
                else
                {
                    retorno.ret_code = ret.code;
                    retorno.ret_info = ret.message;
                }
            }
            catch (Exception ex)
            {
                retorno.ret_code = -105;
                retorno.ret_info = ex.Message;
            }

            return retorno;
        }
    }
}