using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using Dapper;
using System.Linq;

namespace SolutionsTools.DAL
{
    public class FuncaoUsuarioDAL : DBConn
    {
        public long Insert(FuncaoUsuario funcaoUsuario)
        {
            using (var conn = Connection())
            {
                return conn.Insert<FuncaoUsuario>(funcaoUsuario);
            }
        }
        public void Update(FuncaoUsuario funcaoUsuario)
        {
            using (var conn = Connection())
            {
                conn.Update<FuncaoUsuario>(funcaoUsuario);
            }
        }
        public void Delete(FuncaoUsuario funcaoUsuario)
        {
            using (var conn = Connection())
            {
                conn.Delete<FuncaoUsuario>(funcaoUsuario);
            }
        }
        public void DeleteByUser(int id_Usuario)
        {
            using (var conn = Connection())
            {
                conn.Execute(string.Format("delete from tb_funcao_usuario where Id_Usuario = {0}", id_Usuario));
            }
        }
        public IEnumerable<FuncaoUsuario> GetByUser(int id_Usuario)
        {
            using (var conn = Connection())
            {
                var funcoes = conn.Query<FuncaoUsuario>(string.Format("select * from tb_funcao_usuario where Id_Usuario = {0}", id_Usuario));

                if (funcoes.Count() > 0)
                {
                    return funcoes;
                }

                return new List<FuncaoUsuario>();
            }
        }
        public IEnumerable<FuncaoUsuario> GetByFunction(int id_Funcao)
        {
            using (var conn = Connection())
            {
                var funcoes = conn.Query<FuncaoUsuario>(string.Format("select * from tb_funcao_usuario where Id_Funcao = {0}", id_Funcao));

                if (funcoes.Count() > 0)
                {
                    return funcoes;
                }

                return new List<FuncaoUsuario>();
            }
        }
    }
}