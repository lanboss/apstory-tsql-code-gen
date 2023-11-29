using System;
using Dapper;
using System.Data;
using System.Data.SqlClient;
using test1.Dal.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;
using test1.Common.Util;

namespace test1.Dal.Dapper
{
    public partial class *gen_ExcludeSchemaTableNameRepository : BaseRepository, I*gen_ExcludeSchemaTableNameRepository
    {
        public *gen_ExcludeSchemaTableNameRepository(string connectionString) : base(connectionString) { }
        public async Task<test1.Model.*gen_ExcludeSchemaTableName> InsUpd*gen_ExcludeSchemaTableName(test1.Model.*gen_ExcludeSchemaTableName *gen_ExcludeSchemaTableName)
        {
            test1.Model.*gen_ExcludeSchemaTableName ret*gen_ExcludeSchemaTableName = new test1.Model.*gen_ExcludeSchemaTableName();
            DynamicParameters dParams = new DynamicParameters();
            dParams.Add("RetVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            using (SqlConnection connection = GetConnection())
            {
                ret*gen_ExcludeSchemaTableName = await connection.QueryFirstOrDefaultAsync<test1.Model.*gen_ExcludeSchemaTableName>("dbo.zgen_*gen_ExcludeSchemaTableName_InsUpd", dParams, commandType: System.Data.CommandType.StoredProcedure);
            }
            string retMsg = dParams.Get<string>("RetMsg");
            int retVal = dParams.Get<int>("RetVal");
            if (retVal == 1) { throw new Exception(retMsg); }
            return ret*gen_ExcludeSchemaTableName;
        }
        public async Task<List<test1.Model.*gen_ExcludeSchemaTableName>> Get*gen_ExcludeSchemaTableNameByI)
        {
            List<test1.Model.*gen_ExcludeSchemaTableName> ret*gen_ExcludeSchemaTableName = new List<test1.Model.*gen_ExcludeSchemaTableName>();
            DynamicParameters dParams = new DynamicParameters();
            using (SqlConnection connection = GetConnection())
            {
                ret*gen_ExcludeSchemaTableName = (await connection.QueryAsync<test1.Model.*gen_ExcludeSchemaTableName>("dbo.zgen_*gen_ExcludeSchemaTableName_GetById", dParams, commandType: System.Data.CommandType.StoredProcedure)).AsList();
            }
            return ret*gen_ExcludeSchemaTableName;
        }
        public async Task Del*gen_ExcludeSchemaTableNameHr)
        {
            DynamicParameters dParams = new DynamicParameters();
            dParams.Add("RetVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            using (SqlConnection connection = GetConnection())
            {
                await connection.ExecuteAsync("dbo.zgen_*gen_ExcludeSchemaTableName_DelHrd", dParams, commandType: System.Data.CommandType.StoredProcedure);
            }
            string retMsg = dParams.Get<string>("RetMsg");
            int retVal = dParams.Get<int>("RetVal");
            if (retVal == 1) { throw new Exception(retMsg); }
        }
    }
}