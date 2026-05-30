using System.Configuration;
using System.Data.SqlClient;

namespace SolutionsTools.DAL
{
    public class DBConn
    {
        private string _conn = ConfigurationManager.ConnectionStrings["DB_Conn"].ConnectionString;

        public SqlConnection Connection() {
            var conn = new SqlConnection(_conn);
            return conn;
        }

    }
}
