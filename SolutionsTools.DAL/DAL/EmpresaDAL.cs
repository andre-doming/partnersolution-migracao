using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using Dapper;
using System.Linq;


namespace SolutionsTools.DAL
{
    public class EmpresaDAL : DBConn
    {

        public long Insert(Empresa empresa)
        {
            using (var conn = Connection())
            {
                return conn.Insert<Empresa>(empresa);
            }
        }

        public void Update(Empresa empresa)
        {
            using (var conn = Connection())
            {
                conn.Update<Empresa>(empresa);
            }
        }

        public void Delete(Empresa empresa)
        {
            using (var conn = Connection())
            {
                conn.Delete<Empresa>(empresa);
            }
        }

        public Empresa GetById(int id)
        {
            using (var conn = Connection())
            {
                return conn.Get<Empresa>(id);
            }
        }

        public Empresa GetByCnpj(string cnpj)
        {
            using (var conn = Connection())
            {
                var empresa = conn.Query<Empresa>(string.Format("select * from tb_empresa where cnpj = '{0}'", cnpj));

                if (empresa.Count() > 0)
                {
                    return empresa.First();
                }

                return new Empresa();
            }
        }

        public IEnumerable<Empresa> GetCompanies()
        {
            using (var conn = Connection())
            {
                var empresas = conn.Query<Empresa>(string.Format("select * from tb_empresa"));

                if (empresas.Count() > 0)
                {
                    return empresas;
                }

                return new List<Empresa>();
            }
        }

        public List<Empresa> GetByCompanyAndCustom(string field, string value)
        {
            var empresas = new List<Empresa>();

            using (var conn = Connection())
            {
                var query = string.Format("select * from Tb_Empresa where {0} like '%{1}%'", field, value);

                empresas = conn.Query<Empresa>(query).ToList();

                return empresas;
            }
        }
    }
}
