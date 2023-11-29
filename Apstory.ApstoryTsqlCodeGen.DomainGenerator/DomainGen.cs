using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apstory.ApstoryTsqlCodeGen.Shared.Interfaces;
using Apstory.ApstoryTsqlCodeGen.Shared.Service;
using Apstory.ApstoryTsqlCodeGen.Shared.Models;

namespace Apstory.ApstoryTsqlCodeGen.DomainGenerator
{
    public partial class DomainGen : BaseGenerator
    {
        public DomainGen(ISqlTablesRepository tableRepository, string genPath, string modelGenPath, string genPathNamespace, bool threaded)
            : base(tableRepository, genPath, modelGenPath, genPathNamespace, threaded)
        { }

        public async Task Run(string path, string classNamespace, string schema, bool includeForeignKeys)
        {
            var tables = await _TableRepository.GetDBTables(schema);
            var tablesWithIndexes = await _TableRepository.GetDBTablesWithIndexes(schema);

            try
            {
                if (_Threaded)
                {
                    Parallel.ForEach(tables, (table) =>
                    {
                        GenerateDomainTable(table, path, classNamespace, schema).Wait();

                        if (includeForeignKeys)
                            GenerateDomainTableIncludeForeignKeys(table, path, classNamespace, schema).Wait();
                    });

                    Parallel.ForEach(tablesWithIndexes, (tableWithIndex) =>
                    {
                        GenerateDomainIndexedTable(tableWithIndex, path, classNamespace, schema).Wait();

                        if (includeForeignKeys)
                            GenerateDomainIndexedTableIncludeForeignKeys(tableWithIndex, path, classNamespace, schema).Wait();
                    });
                }
                else
                {
                    foreach (var table in tables)
                    {
                        await GenerateDomainTable(table, path, classNamespace, schema);

                        if (includeForeignKeys)
                            await GenerateDomainTableIncludeForeignKeys(table, path, classNamespace, schema);
                    }

                    foreach (var tableWithIndex in tablesWithIndexes)
                    {
                        await GenerateDomainIndexedTable(tableWithIndex, path, classNamespace, schema);

                        if (includeForeignKeys)
                            await GenerateDomainIndexedTableIncludeForeignKeys(tableWithIndex, path, classNamespace, schema);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }

        }

        private async Task GenerateDomainTable(SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                bool addSchemaPath = (schema != "dbo");

                var sb = new StringBuilder();
                sb.Append(AddHeader(classNamespace, table.TABLE_NAME, schema));
                GenerateDomainInsUpd(sb, table, path, classNamespace);
                await GenerateDomainGetById(sb, table, path, classNamespace, schema);
                await GenerateDomainGetByIds(sb, table, path, classNamespace, schema);
                await GenerateDomainGetByIdsPaging(sb, table, path, classNamespace, schema);
                await GenerateDomainGetByNameIds(sb, table, path, classNamespace, schema);
                await GenerateDomainDelHrd(sb, table, path, classNamespace, schema);
                await GenerateDomainDelSft(sb, table, path, classNamespace, schema);
                await GenerateDomainGetBySearch(sb, table, path, classNamespace, schema);
                await GenerateDomainGetBySearchFreeText(sb, table, path, classNamespace, schema);
                sb.Append(AddFooter());
                LogOutputLine();
                LogOutput(sb.ToString());
                LogOutputLine();
                string fileName = table.TABLE_NAME.Replace("*", "_") + "Service" + (addSchemaPath ? "." + schema.ToUpper() : string.Empty) + ".Gen.cs";
                string filePath;
                if (_GenPath.Length > 0)
                    filePath = Path.Join( path, (addSchemaPath ? schema.ToUpper() : string.Empty), _GenPath.Replace(".", ""), fileName);
                else
                    filePath = Path.Join(path, (addSchemaPath ? schema.ToUpper() : string.Empty), fileName);
                Shared.Utils.GeneratorUtils.WriteToFile(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainIndexedTable(SqlTable tableWithIndex, string path, string classNamespace, string schema)
        {
            try
            {
                bool addSchemaPath = (schema != "dbo");

                var sb = new StringBuilder();
                sb.Append(AddHeaderIndex(classNamespace, tableWithIndex.TABLE_NAME, schema));
                await GenerateDomainGetByIndex(sb, tableWithIndex, path, classNamespace, schema);
                sb.Append(AddFooter());
                LogOutputLine();
                LogOutput(sb.ToString());
                LogOutputLine();
                string fileName = tableWithIndex.TABLE_NAME + "Service" + (addSchemaPath ? "." + schema.ToUpper() : string.Empty) + ".Index.Gen.cs";
                string filePath;
                
                if (_GenPath.Length > 0)
                    filePath = Path.Join( path, (addSchemaPath ? schema.ToUpper() : string.Empty), _GenPath.Replace(".", ""), fileName);
                else
                    filePath = Path.Join(path, (addSchemaPath ? schema.ToUpper() : string.Empty), fileName);
                
                Shared.Utils.GeneratorUtils.WriteToFile(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private void GenerateDomainInsUpd(StringBuilder sb, SqlTable table, string path, string classNamespace)
        {
            try
            {
                LogOutputLine("Process domain InsUpd for table " + table.TABLE_NAME);
                sb.Append(AddMethodStartModel(classNamespace, table.TABLE_NAME, "InsUpd", ""));
                sb.Append(AddMethodParamModel(classNamespace, table.TABLE_NAME));
                sb.Append(")" + _NewLine);
                sb.Append(_Tab + _Tab + "{" + _NewLine);
                sb.Append(_Tab + _Tab + _Tab + "return await _repo.InsUpd"
                    + table.TABLE_NAME + "(" + Shared.Utils.GeneratorUtils.CamelCase(table.TABLE_NAME) + ");" + _NewLine);
                sb.Append(_Tab + _Tab + "}" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetById(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process domain GetById for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetById", schema);
                sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ById"));
                foreach (var item in spParams)
                {
                    sb.Append(AddMethodNullableParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(")" + _NewLine);
                sb.Append(_Tab + _Tab + "{" + _NewLine);
                sb.Append(_Tab + _Tab + _Tab + "return await _repo.Get" + table.TABLE_NAME + "ById(");
                foreach (var item in spParams)
                {
                    sb.Append(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1)) + ", ");
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(");" + _NewLine);
                sb.Append(_Tab + _Tab + "}" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetByNameIds(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process domain GetByNameIds for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, $"GetBy{table.TABLE_NAME}Ids", schema);

                if (!spParams.Any())
                {
                    LogOutputLine("Skipped");
                    return;
                }

                var idParam = spParams.FirstOrDefault(s => s.ParameterName == $"@Uniqueidentifiers" || s.ParameterName == $"@Ids");
                if (idParam == null)
                {
                    LogOutputLine("Skipped");
                    return;
                }

                sb.Append($"{_Tab}{_Tab}public async Task<List<{classNamespace}.Model.{_ModelGenPath}{table.TABLE_NAME}>> Get{table.TABLE_NAME}By{table.TABLE_NAME}Ids(");
                foreach (var item in spParams)
                {
                    sb.Append(AddMethodNullableAndNormalParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(")" + _NewLine);
                sb.Append(_Tab + _Tab + "{" + _NewLine);
                sb.Append(_Tab + _Tab + _Tab + $"return await _repo.Get{table.TABLE_NAME}By{table.TABLE_NAME}Ids(");
                foreach (var item in spParams)
                {
                    sb.Append(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1)) + ", ");
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(");" + _NewLine);
                sb.Append(_Tab + _Tab + "}" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetByIds(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine();
                LogOutputLine();
                LogOutputLine("Process domain GetByIds for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetByIds", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ByIds"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodNullableParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append(_Tab + _Tab + _Tab + "return await _repo.Get" + table.TABLE_NAME + "ByIds(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetByIdsPaging(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process domain GetByIdsPaging for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetByIdsPaging", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ByIdsPaging"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodNullableParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append(_Tab + _Tab + _Tab + "return await _repo.Get" + table.TABLE_NAME + "ByIdsPaging(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetBySearch(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process domain GetBySearch for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBySearch", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "BySearch"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append(_Tab + _Tab + _Tab + "return await _repo.Get" + table.TABLE_NAME + "BySearch(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetBySearchFreeText(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process domain GetBySearchFreeText for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBySearchFreeText", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "BySearchFreeText"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append(_Tab + _Tab + _Tab + "return await _repo.Get" + table.TABLE_NAME + "BySearchFreeText(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetByIndex(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process domain GetByIndex for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBy" + table.COLUMN_NAME, schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "By" + table.COLUMN_NAME));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append(_Tab + _Tab + _Tab + "return await _repo.Get" + table.TABLE_NAME + "By" + table.COLUMN_NAME + "(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainDelHrd(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process domain DelHrd for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "DelHrd", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartVoid(classNamespace, table.TABLE_NAME, "Del", "Hrd"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append(_Tab + _Tab + _Tab + "await _repo.Del" + table.TABLE_NAME + "Hrd(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainDelSft(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process domain DelSft for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "DelSft", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartVoid(classNamespace, table.TABLE_NAME, "Del", "Sft"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append(_Tab + _Tab + _Tab + "await _repo.Del" + table.TABLE_NAME + "Sft(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private StringBuilder AddMethodParams(string columnType, string paramName)
        {
            StringBuilder sb = new StringBuilder();
            if (paramName != "retMsg")
            {
                sb.Append(columnType + " " + paramName + ", ");
            }
            return sb;
        }

        private StringBuilder AddRepoMethodParams(string paramName)
        {
            StringBuilder sb = new StringBuilder();
            if (paramName != "retMsg")
            {
                sb.Append(paramName + ", ");
            }
            return sb;
        }

        private StringBuilder AddMethodStartList(string classNamespace, string tableName, string storedProcPrefix, string storedProcType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + "public async Task<List<" + classNamespace + ".Model." + _ModelGenPath + tableName + ">> " + storedProcPrefix + tableName + storedProcType + "(");
            return sb;
        }

        private StringBuilder AddMethodStartVoid(string classNamespace, string tableName, string storedProcPrefix, string storedProcType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + "public async Task " + storedProcPrefix + tableName + storedProcType + "(");
            return sb;
        }

        private StringBuilder AddMethodStartModel(string classNamespace, string tableName, string storedProcPrefix, string storedProcType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + "public async Task<" + classNamespace + ".Model." + _ModelGenPath + tableName + "> " + storedProcPrefix + tableName + storedProcType + "(");
            return sb;
        }

        private StringBuilder AddMethodParamModel(string classNamespace, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(classNamespace + ".Model." + _ModelGenPath + tableName + " " + Shared.Utils.GeneratorUtils.CamelCase(tableName));
            return sb;
        }

        private StringBuilder AddMethodNullableParams(string columnType, string paramName)
        {
            StringBuilder sb = new StringBuilder();
            if (paramName != "retMsg")
            {
                if (columnType == "string" || columnType == "Microsoft.SqlServer.Types.SqlGeography")
                {
                    if (paramName.ToLower() == "sortdirection")
                    {
                        sb.Append(columnType + " " + paramName + " = \"ASC\", ");
                    }
                    else
                    {
                        sb.Append(columnType + " " + paramName + ", ");
                    }
                }
                else
                {
                    sb.Append(columnType + "? " + paramName + ", ");
                }
            }
            return sb;
        }

        private StringBuilder AddMethodNullableAndNormalParams(string columnType, string paramName)
        {
            StringBuilder sb = new StringBuilder();
            if (paramName != "retMsg")
            {
                if (paramName == "uniqueidentifiers" || paramName == "ids")
                    sb.Append(columnType + " " + paramName + ", ");
                else
                    sb.Append(columnType + "? " + paramName + ", ");
            }
            return sb;
        }

        private StringBuilder AddHeader(string classNamespace, string tableName, string schema)
        {
            bool addSchema = (schema != "dbo");
            StringBuilder sb = new StringBuilder();
            sb.Append("using System;" + _NewLine +
                "using " + classNamespace + ".Dal.Interface" + (addSchema ? "." + schema.ToUpper() : "") + _GenPathNamespace + ";" + _NewLine +
                "using " + classNamespace + ".Domain.Interface" + (addSchema ? "." + schema.ToUpper() : "") + _GenPathNamespace + ";" + _NewLine +
                "using System.Collections.Generic;" + _NewLine +
                "using System.Threading.Tasks;" + _NewLine + _NewLine +
                "namespace " + classNamespace + ".Domain" + (addSchema ? "." + schema.ToUpper() : "") + _GenPathNamespace + _NewLine +
                "{" + _NewLine +
                _Tab + "public partial class " + tableName + "Service : I" + tableName + "Service" + _NewLine +
                _Tab + "{" + _NewLine +
                _Tab + _Tab + "private readonly I" + tableName + "Repository _repo;" + _NewLine +
                _Tab + _Tab + "public " + tableName + "Service(I" + tableName + "Repository repo)" + _NewLine +
                _Tab + _Tab + "{" + _NewLine +
                _Tab + _Tab + _Tab + "_repo = repo;" + _NewLine +
                _Tab + _Tab + "}" + _NewLine
                );
            return sb;
        }

        private StringBuilder AddHeaderIndex(string classNamespace, string tableName, string schema)
        {
            bool addSchema = (schema != "dbo");

            StringBuilder sb = new StringBuilder();
            sb.Append("using System;" + _NewLine +
                "using " + classNamespace + ".Dal.Interface" + (addSchema ? "." + schema.ToUpper() : "") + _GenPathNamespace + ";" + _NewLine +
                "using " + classNamespace + ".Domain.Interface" + (addSchema ? "." + schema.ToUpper() : "") + _GenPathNamespace + ";" + _NewLine +
                "using System.Collections.Generic;" + _NewLine +
                "using System.Threading.Tasks;" + _NewLine + _NewLine +
                "namespace " + classNamespace + ".Domain" + (addSchema ? "." + schema.ToUpper() : "") + _GenPathNamespace + _NewLine +
                "{" + _NewLine +
                _Tab + "public partial class " + tableName + "Service : I" + tableName + "Service" + _NewLine +
                _Tab + "{" + _NewLine
                );
            return sb;
        }

        private StringBuilder AddFooter()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + "}");
            sb.Append(_NewLine + "}");
            return sb;
        }
    }
}
