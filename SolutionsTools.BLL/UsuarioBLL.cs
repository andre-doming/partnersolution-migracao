using SolutionsTools.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionsTools.BLL
{
    public class UsuarioBLL
    {
        public bool ExisteLogin(string login)
        {
            var dto = new UsuarioDAL();

            var usuario = dto.GetByLogin(login);

            return (usuario != null);
        }
    }
}
