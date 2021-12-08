using Apstory.ApstoryTsqlCodeGen.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Apstory.ApstoryTsqlCodeGen.Shared.Interfaces
{
    public interface ISqlTablesRepository
    {
        Task<List<SqlTable>> GetDBTables(string schema = "dbo");
        Task<List<SqlTable>> GetDBTablesWithIndexes(string schema = "dbo");
        Task<string> GetTableModel(string tableName, string schema = "dbo", bool convertLongsToString = false, bool includeForeignKeys = false);
        Task<List<SqlTableColumn>> GetTableColumnsByTableName(string tableName, string schema = "dbo");
        Task<List<SqlStoredProcedureParams>> GetAllStoredProcParams(string prefix, string schema = "dbo");
        Task<List<SqlStoredProcedureParams>> GetStoredProcParams(string prefix, string tableName, string suffix, string schema = "dbo");
        Task<List<string>> GetGeneratorStoredProcCreateScript();
        Task AddGeneratorStoredProcs(string script);
        Task ExecuteCreateAllGeneratorStoredProcedures(string schema = "dbo");
    }
}
