using System.Data.SqlClient;

namespace test1.Dal.Dapper
{
    public partial class BaseRepository
    {
        private string _ConnectionString;

        public BaseRepository(string connectionString)
        {
            _ConnectionString = connectionString;
        }
        protected SqlConnection GetConnection()
        {
            return new SqlConnection(_ConnectionString);
        }
    }
}