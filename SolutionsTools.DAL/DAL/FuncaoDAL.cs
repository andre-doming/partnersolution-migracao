using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using Dapper;
using System.Linq;

namespace SolutionsTools.DAL
{
    public class FuncaoDAL : DBConn
    {
        public long Insert(Funcao funcao)
        {
            using (var conn = Connection())
            {
                return conn.Insert<Funcao>(funcao);
            }
        }
        public void Update(Funcao funcao)
        {
            using (var conn = Connection())
            {
                conn.Update<Funcao>(funcao);
            }
        }
        public void Delete(Funcao funcao)
        {
            using (var conn = Connection())
            {
                conn.Delete<Funcao>(funcao);
            }
        }
        public Funcao GetById(int id)
        {
            using (var conn = Connection())
            {
                return conn.Get<Funcao>(id);
            }
        }

        public IEnumerable<Funcao> GetFunctions()
        {
            using (var conn = Connection())
            {
                var funcoes = conn.Query<Funcao>(string.Format("select * from tb_funcao where Ativo = 'S'"));

                if (funcoes.Count() > 0)
                {
                    return funcoes;
                }

                return new List<Funcao>();
            }
        }
    }
}