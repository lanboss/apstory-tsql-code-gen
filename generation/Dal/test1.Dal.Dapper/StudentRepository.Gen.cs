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
    public partial class StudentRepository : BaseRepository, IStudentRepository
    {
        public StudentRepository(string connectionString) : base(connectionString) { }
        public async Task<test1.Model.Student> InsUpdStudent(test1.Model.Student student)
        {
            test1.Model.Student retStudent = new test1.Model.Student();
            DynamicParameters dParams = new DynamicParameters();
            dParams.Add("RetVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            using (SqlConnection connection = GetConnection())
            {
                retStudent = await connection.QueryFirstOrDefaultAsync<test1.Model.Student>("dbo.zgen_Student_InsUpd", dParams, commandType: System.Data.CommandType.StoredProcedure);
            }
            string retMsg = dParams.Get<string>("RetMsg");
            int retVal = dParams.Get<int>("RetVal");
            if (retVal == 1) { throw new Exception(retMsg); }
            return retStudent;
        }
        public async Task<List<test1.Model.Student>> GetStudentByI)
        {
            List<test1.Model.Student> retStudent = new List<test1.Model.Student>();
            DynamicParameters dParams = new DynamicParameters();
            using (SqlConnection connection = GetConnection())
            {
                retStudent = (await connection.QueryAsync<test1.Model.Student>("dbo.zgen_Student_GetById", dParams, commandType: System.Data.CommandType.StoredProcedure)).AsList();
            }
            return retStudent;
        }
        public async Task DelStudentHr)
        {
            DynamicParameters dParams = new DynamicParameters();
            dParams.Add("RetVal", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            using (SqlConnection connection = GetConnection())
            {
                await connection.ExecuteAsync("dbo.zgen_Student_DelHrd", dParams, commandType: System.Data.CommandType.StoredProcedure);
            }
            string retMsg = dParams.Get<string>("RetMsg");
            int retVal = dParams.Get<int>("RetVal");
            if (retVal == 1) { throw new Exception(retMsg); }
        }
    }
}