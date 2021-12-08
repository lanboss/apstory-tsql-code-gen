using System.Data.SqlClient;

namespace Apstory.ApstoryTsqlCodeGen.Shared.Repositories
{
    public class BaseRepository
    {
        private readonly string _ConnectionString;

        public BaseRepository(string connectionString)
        {
            _ConnectionString = connectionString;
        }

        protected SqlConnection GetConnection()
        {
            var sqlConnection = new SqlConnection(_ConnectionString);
            return sqlConnection;
        }
    }
}
