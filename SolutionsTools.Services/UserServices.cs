using SolutionsTools.DAL;
using System;

namespace SolutionsTools.Services
{
    public class UserServices
    {
        public ReturnUser Authenticate(string user, string password)
        {
            var ret = new ReturnUser();

            try
            {
                var DTO = new UsuarioDAL();
                var usuario = DTO.GetByLogin(user);

                if (usuario == null)
                {
                    ret.status = false;
                    ret.code = -100;
                    ret.message = "Usuário não encontrado";
                }
                else
                {
                    if (usuario.Acesso_Token.Equals("N"))
                    {
                        ret.status = false;
                        ret.code = -101;
                        ret.message = "Usuário inválido para acesso a token";
                    }
                    else
                    {
                        var crip = new CryptographyMD5();

                        if (crip.CompareMD5(usuario.Senha, password))
                        {
                            ret.status = true;
                            ret.code = 0;
                            ret.message = string.Empty;
                        }
                        else
                        {
                            ret.status = false;
                            ret.code = -102;
                            ret.message = "Senha incorreta";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret.status = false;
                ret.code = -103;
                ret.message = ex.Message;
            }

            return ret;
        }
    }
}
