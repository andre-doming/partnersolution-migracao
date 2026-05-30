using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using Dapper;
using System.Linq;
using System.Data;

namespace SolutionsTools.DAL
{
    public class LogDAL : DBConn
    {
        public long Insert(Log log)
        {
            using (var conn = Connection())
            {
                return conn.Insert<Log>(log);
            }
        }        
        
        public long InsertByProc(string login, string entidade, string guid_cliente, string movimentacao)
        {
            using (var conn = Connection())
            {   
                var p = new DynamicParameters();
                p.Add("p_login", login);
                p.Add("p_entidade", entidade);
                p.Add("p_guid_cliente", guid_cliente);
                p.Add("p_movimentacao", movimentacao);
                return conn.Execute("SP_INS_TB_LOG", p, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
