using Apstory.ApstoryTsqlCodeGen.Shared.Interfaces;
using Apstory.ApstoryTsqlCodeGen.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Apstory.ApstoryTsqlCodeGen.Shared.Repositories
{
    public class SqlTablesRepository : BaseRepository, ISqlTablesRepository
    {
        public SqlTablesRepository(string connectionString) : base(connectionString)
        { }

        public async Task<List<SqlTable>> GetDBTables(string schema = "dbo")
        {
            List<SqlTable> table = new List<SqlTable>();
            DynamicParameters parms = new DynamicParameters();
            parms.Add("Schema", schema);
            using (SqlConnection connection = GetConnection())
            {
                table = (await connection.QueryAsync<SqlTable>("[dbo].[*gen_ApstoryStoredProcGeneratorGetTables]", param: parms, commandType: CommandType.StoredProcedure)).AsList();
            }
            return table;
        }

        public async Task<List<SqlTable>> GetDBTablesWithIndexes(string schema = "dbo")
        {
            List<SqlTable> table = new List<SqlTable>();
            DynamicParameters parms = new DynamicParameters();
            parms.Add("Schema", schema);
            using (SqlConnection connection = GetConnection())
            {
                table = (await connection.QueryAsync<SqlTable>("[dbo].[*gen_ApstoryStoredProcGeneratorGetTablesWithIndexes]", param: parms, commandType: CommandType.StoredProcedure)).AsList();
            }
            return table;
        }

        public async Task<string> GetTableModel(string tableName, string schema = "dbo", bool convertLongsToString = false, bool includeForeignKeys = false)
        {
            string model;
            DynamicParameters param = new DynamicParameters();
            param.Add("TableName", tableName);
            param.Add("Schema", schema);
            param.Add("IncludeJsonConvert", convertLongsToString);
            param.Add("IncludeForeignKeys", includeForeignKeys);
            param.Add("Model", string.Empty, dbType: DbType.String, direction: ParameterDirection.Output);
            using (SqlConnection connection = GetConnection())
            {
                await connection.QueryFirstAsync<string>("[dbo].[*gen_ApstoryModelGeneratorByTableName]", param: param, commandType: System.Data.CommandType.StoredProcedure);
            }
            model = param.Get<string>("Model");
            return model;
        }

        public async Task<List<SqlTableColumn>> GetTableColumnsByTableName(string tableName, string schema = "dbo")
        {
            List<SqlTableColumn> columns = new List<SqlTableColumn>();

            string model;
            DynamicParameters param = new DynamicParameters();
            param.Add("TableName", tableName);
            param.Add("Schema", schema);
            using (SqlConnection connection = GetConnection())
            {
                columns = (await connection.QueryAsync<SqlTableColumn>("[dbo].[*gen_ApstoryGetTableColumnsByTableName]", param: param, commandType: System.Data.CommandType.StoredProcedure)).AsList();
            }
            return columns;
        }


        public async Task<List<SqlStoredProcedureParams>> GetAllStoredProcParams(string prefix, string schema = "dbo")
        {
            List<SqlStoredProcedureParams> spParams = new List<SqlStoredProcedureParams>();
            DynamicParameters parms = new DynamicParameters();
            parms.Add("Prefix", prefix);
            parms.Add("Schema", schema);
            using (SqlConnection connection = GetConnection())
            {
                spParams = (await connection.QueryAsync<SqlStoredProcedureParams>("[*gen_ApstoryStoredProcGeneratorGetAllParams]", param: parms, commandType: CommandType.StoredProcedure)).AsList();
            }
            return spParams;
        }

        public async Task<List<SqlStoredProcedureParams>> GetStoredProcParams(string prefix, string tableName, string suffix, string schema = "dbo")
        {
            List<SqlStoredProcedureParams> spParams = new List<SqlStoredProcedureParams>();
            DynamicParameters parms = new DynamicParameters();
            parms.Add("Prefix", prefix);
            parms.Add("TableName", tableName);
            parms.Add("Suffix", suffix);
            parms.Add("Schema", schema);
            using (SqlConnection connection = GetConnection())
            {
                spParams = (await connection.QueryAsync<SqlStoredProcedureParams>("[*gen_ApstoryStoredProcGeneratorGetParamsByTable]", param: parms, commandType: CommandType.StoredProcedure)).AsList();
            }
            return spParams;
        }


        public async Task<List<string>> GetGeneratorStoredProcCreateScript()
        {
            List<string> createScripts;
            using (SqlConnection connection = GetConnection())
            {
                createScripts = (await connection.QueryAsync<string>("[dbo].[*gen_ApstoryStoredProcGeneratorGetGenSPs]", commandType: CommandType.StoredProcedure)).AsList();
            }
            return createScripts;
        }

        public async Task AddGeneratorStoredProcs(string script)
        {
            using (SqlConnection connection = GetConnection())
            {
                await connection.ExecuteAsync(script);
            }
        }

        public async Task ExecuteCreateAllGeneratorStoredProcedures(string schema = "dbo")
        {
            DynamicParameters parms = new DynamicParameters();
            parms.Add("Schema", schema);
            using (SqlConnection connection = GetConnection())
            {
                await connection.ExecuteAsync("[dbo].[*gen_ApstoryStoredProcGeneratorCreateAll]", param: parms, commandType: CommandType.StoredProcedure, commandTimeout: 480);
            }
        }
    }
}
