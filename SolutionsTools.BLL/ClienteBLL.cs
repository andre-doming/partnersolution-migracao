using SolutionsTools.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionsTools.BLL
{
    public class ClienteBLL
    {
        public bool ExisteCPF(string cpf)
        {
            var dto = new ClienteDAL();

            var cliente = dto.GetByCpf(cpf);

            return (cliente != null);
        }


    }
}
