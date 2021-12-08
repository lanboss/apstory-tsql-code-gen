using Apstory.ApstoryTsqlCodeGen.Shared.Interfaces;
using Apstory.ApstoryTsqlCodeGen.Shared.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Apstory.ApstoryTsqlCodeGen.Shared.Repositories
{
    public class CachedSqlTablesRepository : ISqlTablesRepository
    {
        private SqlTablesRepository sqlTablesRepository;
        private ConcurrentDictionary<string, object> cache;

        public CachedSqlTablesRepository(string connectionString)
        {
            sqlTablesRepository = new SqlTablesRepository(connectionString);
            cache = new ConcurrentDictionary<string, object>();
        }

        private T GetCache<T>(string key) where T : class
        {
            object outValue;
            if (cache.TryGetValue(key, out outValue))
                return outValue as T;

            return null;
        }

        private bool HasCache(string key)
        {
            return cache.ContainsKey(key);
        }

        private bool AddCache(string key, object value)
        {
            return cache.TryAdd(key, value);
        }

        public async Task AddGeneratorStoredProcs(string script)
        {
            await sqlTablesRepository.AddGeneratorStoredProcs(script);
        }

        public async Task ExecuteCreateAllGeneratorStoredProcedures(string schema = "dbo")
        {
            await sqlTablesRepository.ExecuteCreateAllGeneratorStoredProcedures(schema);
        }

        public async Task<List<SqlTable>> GetDBTables(string schema = "dbo")
        {
            var key = $"GetDBTables_{schema}";
            if (!HasCache(key))
                AddCache(key, await sqlTablesRepository.GetDBTables(schema));

            return GetCache<List<SqlTable>>(key);
        }

        public async Task<List<SqlTable>> GetDBTablesWithIndexes(string schema = "dbo")
        {
            var key = $"GetDBTablesWithIndexes_{schema}";
            if (!HasCache(key))
                AddCache(key, await sqlTablesRepository.GetDBTablesWithIndexes(schema));

            return GetCache<List<SqlTable>>(key);
        }

        public async Task<List<string>> GetGeneratorStoredProcCreateScript()
        {
            var key = $"GetGeneratorStoredProcCreateScript";
            if (!HasCache(key))
                AddCache(key, await sqlTablesRepository.GetGeneratorStoredProcCreateScript());

            return GetCache<List<string>>(key);
        }

        public async Task<string> GetTableModel(string tableName, string schema = "dbo", bool convertLongsToString = false, bool includeForeignKeys = false)
        {
            var key = $"GetTableModel_{tableName}_{schema}_{convertLongsToString}";
            if (!HasCache(key))
                AddCache(key, await sqlTablesRepository.GetTableModel(tableName, schema, convertLongsToString, includeForeignKeys));

            return GetCache<string>(key);
        }

        public async Task<List<SqlTableColumn>> GetTableColumnsByTableName(string tableName, string schema = "dbo")
        {
            var key = $"GetTableColumnsByTableName_{tableName}_{schema}";
            if (!HasCache(key))
                AddCache(key, await sqlTablesRepository.GetTableColumnsByTableName(tableName, schema));

            return GetCache<List<SqlTableColumn>>(key);
        }

        public async Task<List<SqlStoredProcedureParams>> GetAllStoredProcParams(string prefix, string schema = "dbo")
        {
            var key = $"GetAllStoredProcParams_{prefix}_{schema}";
            if (!HasCache(key))
                AddCache(key, await sqlTablesRepository.GetAllStoredProcParams(prefix, schema));

            return GetCache<List<SqlStoredProcedureParams>>(key);
        }

        private Dictionary<string, List<SqlStoredProcedureParams>> storedProcCache;
        public async Task WarmUp(string prefix, string schema = "dbo")
        {
            var allParameters = await this.GetAllStoredProcParams(prefix, schema);
            storedProcCache = allParameters.GroupBy(s => s.RoutineName).ToDictionary(s => s.Key, s => s.ToList());
        }

        public async Task<List<SqlStoredProcedureParams>> GetStoredProcParams(string prefix, string tableName, string suffix, string schema = "dbo")
        {
            if (storedProcCache != null)
            {
                var spKey = $"{prefix}_{tableName}_{suffix}";
                if (storedProcCache.ContainsKey(spKey))
                    return storedProcCache[spKey];

                return new List<SqlStoredProcedureParams>();
            }

            var key = $"GetStoredProcParams_{prefix}_{tableName}_{suffix}_{schema}";
            if (!HasCache(key))
                AddCache(key, await sqlTablesRepository.GetStoredProcParams(prefix, tableName, suffix, schema));

            return GetCache<List<SqlStoredProcedureParams>>(key);
        }
    }
}
