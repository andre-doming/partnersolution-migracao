using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using Dapper;
using System.Linq;

namespace SolutionsTools.DAL
{
    public class EmpresaUsuarioDAL : DBConn
    {
        public long Insert(EmpresaUsuario empresaUsuario)
        {
            using (var conn = Connection())
            {
                return conn.Insert<EmpresaUsuario>(empresaUsuario);
            }
        }
        public void Delete(EmpresaUsuario empresaUsuario)
        {
            using (var conn = Connection())
            {
                conn.Query<EmpresaUsuario>(string.Format("delete from tb_empresa_usuario where Id_Usuario = {0} and Id_Empresa = {1}", empresaUsuario.Id_Usuario, empresaUsuario.Id_Empresa));
            }
        }
        public void DeleteByUser(int id_Usuario)
        {
            using (var conn = Connection())
            {
                //conn.Query<EmpresaUsuario>(string.Format("delete from tb_empresa_usuario where Id_Usuario = {0}", id_Usuario));
                conn.Execute(string.Format("delete from tb_empresa_usuario where Id_Usuario = {0}", id_Usuario));
            }
        }
        public IEnumerable<EmpresaUsuario> GetByCompany(int id_Empresa)
        {
            using (var conn = Connection())
            {
                var empresas = conn.Query<EmpresaUsuario>(string.Format("select * from tb_empresa_usuario where Id_Empresa = {0}", id_Empresa));

                if (empresas.Count() > 0)
                {
                    return empresas;
                }

                return new List<EmpresaUsuario>();
            }
        }

        public IEnumerable<EmpresaUsuario> GetByUser(int id_Usuario)
        {
            using (var conn = Connection())
            {
                var empresas = conn.Query<EmpresaUsuario>(string.Format("select * from tb_empresa_usuario where Id_Usuario = {0}", id_Usuario));

                if (empresas.Count() > 0)
                {
                    return empresas;
                }

                return new List<EmpresaUsuario>();
            }
        }
    }
}