using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using Dapper;
using System.Linq;
using System.Configuration;

namespace SolutionsTools.DAL
{
    public class UsuarioDAL : DBConn
    {
        public long Insert(Usuario usuario)
        {
            using (var conn = Connection())
            {
                return conn.Insert<Usuario>(usuario);
            }
        }
        public void Update(Usuario usuario)
        {
            using (var conn = Connection())
            {
                conn.Update<Usuario>(usuario);
            }
        }
        public void Delete(Usuario usuario)
        {
            using (var conn = Connection())
            {
                conn.Delete<Usuario>(usuario);
            }
        }
        public Usuario GetById(int id)
        {
            using (var conn = Connection())
            {
                return conn.Get<Usuario>(id);
            }
        }
        public Usuario GetByLogin(string login)
        {
            using (var conn = Connection())
            {
                var usuarios = conn.Query<Usuario>(string.Format("select * from tb_usuario where Ativo = 'S' and login = '{0}'", login));

                if (usuarios.Count() > 0)
                {
                    return usuarios.First();
                }

                return null;
            }
        }
        public IEnumerable<Usuario> GetByCompany(int id_Empresa)
        {
            using (var conn = Connection())
            {
                var usuarios = conn.Query<Usuario>(string.Format("select * from tb_usuario " +
                                                                 "where Id in (select id_usuario from tb_empresa_usuario where (id_Empresa = {0})) or {0}=0", id_Empresa));

                if (usuarios.Count() > 0)
                {
                    return usuarios;
                }

                return null;
            }
        }

        public List<Usuario> GetByCompanyAndCustom(int id_Empresa, string field, string value)
        {
            var usuarios = new List<Usuario>();

            using (var conn = Connection())
            {
                var query = string.Format("select * from tb_usuario " +
                                          "where Id in (select id_usuario from tb_empresa_usuario where ((id_Empresa = {0})) or {0}=0) " +
                                          "and {1} like '%{2}%'", id_Empresa, field, value);

                usuarios = conn.Query<Usuario>(query).ToList();

                return usuarios;
            }
        }
    }
}