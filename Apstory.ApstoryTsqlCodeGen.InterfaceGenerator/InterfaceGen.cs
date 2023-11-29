using Apstory.ApstoryTsqlCodeGen.Shared.Interfaces;
using Apstory.ApstoryTsqlCodeGen.Shared.Service;
using Apstory.ApstoryTsqlCodeGen.Shared.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apstory.ApstoryTsqlCodeGen.InterfaceGenerator
{
    public partial class InterfaceGen : BaseGenerator
    {
        public InterfaceGen(ISqlTablesRepository tableRepository, string genPath, string modelGenPath, string genPathNamespace, bool threaded)
            : base(tableRepository, genPath, modelGenPath, genPathNamespace, threaded)
        { }

        public async Task Run(string path, string type, string classNamespace, string schema, bool includeForeignKeys)
        {
            var tables = await _TableRepository.GetDBTables(schema);
            var tablesWithIndexes = await _TableRepository.GetDBTablesWithIndexes(schema);

            try
            {
                if (_Threaded)
                {
                    Parallel.ForEach(tables, (table) =>
                    {
                        GenerateInterfaceTable(table, path, type, classNamespace, schema).Wait();

                        if (includeForeignKeys)
                            GenerateInterfaceTableIncludeForeignKeys(table, path, type, classNamespace, schema).Wait();
                    });

                    Parallel.ForEach(tablesWithIndexes, (tableWithIndex) =>
                    {
                        GenerateInterfaceIndexedTable(tableWithIndex, path, type, classNamespace, schema).Wait();

                        if (includeForeignKeys)
                            GenerateInterfaceIndexedTableIncludeForeignKeys(tableWithIndex, path, type, classNamespace, schema).Wait();
                    });
                }
                else
                {
                    foreach (var table in tables)
                    {
                        await GenerateInterfaceTable(table, path, type, classNamespace, schema);

                        if (includeForeignKeys)
                            await GenerateInterfaceTableIncludeForeignKeys(table, path, type, classNamespace, schema);
                    }

                    foreach (var tableWithIndex in tablesWithIndexes)
                    {
                        await GenerateInterfaceIndexedTable(tableWithIndex, path, type, classNamespace, schema);

                        if (includeForeignKeys)
                            await GenerateInterfaceIndexedTableIncludeForeignKeys(tableWithIndex, path, type, classNamespace, schema);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceTable(SqlTable table, string path, string type, string classNamespace, string schema)
        {
            try
            {
                bool addSchemaPath = (schema != "dbo");

                var sb = new StringBuilder();
                sb.Append(AddInterfaceHeader(classNamespace, table.TABLE_NAME, type, schema));
                GenerateInterfaceInsUpd(sb, table, path, classNamespace);
                await GenerateInterfaceGetById(sb, table, path, classNamespace, schema);
                await GenerateInterfaceGetByIds(sb, table, path, classNamespace, schema);
                await GenerateInterfaceGetByIdsPaging(sb, table, path, classNamespace, schema);
                await GenerateInterfaceGetByNameIds(sb, table, path, classNamespace, schema);
                await GenerateInterfaceDelHrd(sb, table, path, classNamespace, schema);
                await GenerateInterfaceDelSft(sb, table, path, classNamespace, schema);
                await GenerateInterfaceGetBySearch(sb, table, path, classNamespace, schema);
                await GenerateInterfaceGetBySearchFreeText(sb, table, path, classNamespace, schema);
                sb.Append(AddInterfaceFooter());
                LogOutputLine();
                LogOutput(sb.ToString());
                LogOutputLine();
                string fileName = "I" + table.TABLE_NAME.Replace("*","_") + type + (addSchemaPath ? "." + schema.ToUpper() : "") + ".Gen.cs";
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


        private async Task GenerateInterfaceIndexedTable(SqlTable tableWithIndex, string path, string type, string classNamespace, string schema)
        {
            try
            {
                bool addSchemaPath = (schema != "dbo");

                var sb = new StringBuilder();
                sb.Append(AddInterfaceHeader(classNamespace, tableWithIndex.TABLE_NAME, type, schema));
                await GenerateInterfaceGetByIndex(sb, tableWithIndex, path, classNamespace, schema);
                sb.Append(AddInterfaceFooter());
                LogOutputLine();
                LogOutput(sb.ToString());
                LogOutputLine();
                string fileName = "I" + tableWithIndex.TABLE_NAME + type + (addSchemaPath ? "." + schema.ToUpper() : "") + ".Index.Gen.cs";
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

        private void GenerateInterfaceInsUpd(StringBuilder sb, SqlTable table, string path, string classNamespace)
        {
            try
            {
                LogOutputLine("Process interface InsUpd for table " + table.TABLE_NAME);
                sb.Append(AddInterfaceMethodStartModel(classNamespace, table.TABLE_NAME, "InsUpd", ""));
                sb.Append(AddInterfaceMethodParamModel(classNamespace, table.TABLE_NAME));
                sb.Append(");" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceGetById(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetById for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetById", schema);
                sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ById"));
                foreach (var item in spParams)
                {
                    sb.Append(AddInterfaceMethodNullableParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(");" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceGetByNameIds(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetByNameIds for table " + table.TABLE_NAME);
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

                sb.Append($"{_Tab}{_Tab}Task<List<{classNamespace}.Model.{_ModelGenPath}{table.TABLE_NAME}>> Get{table.TABLE_NAME}By{table.TABLE_NAME}Ids(");
                foreach (var item in spParams)
                {
                    sb.Append(AddInterfaceMethodNullableAndNormalParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(");" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceGetByIds(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetByIds for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetByIds", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ByIds"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddInterfaceMethodNullableParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceGetByIdsPaging(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetByIdsPaging for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetByIdsPaging", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ByIdsPaging"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddInterfaceMethodNullableParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceGetBySearch(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetBySearch for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBySearch", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "BySearch"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddInterfaceMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceGetBySearchFreeText(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetBySearchFreeText for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBySearchFreeText", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "BySearchFreeText"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddInterfaceMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceGetByIndex(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetByIndex for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBy" + table.COLUMN_NAME, schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "By" + table.COLUMN_NAME));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddInterfaceMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceDelHrd(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface DelHrd for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "DelHrd", schema);
                sb.Append(AddInterfaceMethodStartVoid(classNamespace, table.TABLE_NAME, "Del", "Hrd"));
                foreach (var item in spParams)
                {
                    sb.Append(AddInterfaceMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(");" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceDelSft(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface DelSft for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "DelSft", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartVoid(classNamespace, table.TABLE_NAME, "Del", "Sft"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddInterfaceMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private StringBuilder AddInterfaceHeader(string classNamespace, string tableName, string type, string schema)
        {
            bool addSchema = (schema != "dbo");

            StringBuilder sb = new StringBuilder();
            string subFolder = "";
            if (type == "Repository")
            {
                subFolder = ".Dal";
            }
            else if (type == "Service")
            {
                subFolder = ".Domain";
            }
            sb.Append("using System;" + _NewLine +
                "using System.Collections.Generic;" + _NewLine +
                "using System.Threading.Tasks;" + _NewLine + _NewLine +
                "namespace " + classNamespace + subFolder + ".Interface" + (addSchema ? "." + schema.ToUpper() : "") + _GenPathNamespace + _NewLine +
                "{" + _NewLine +
                _Tab + "public partial interface I" + tableName + type + _NewLine +
                _Tab + "{" + _NewLine);
            return sb;
        }

        private StringBuilder AddInterfaceMethodStartModel(string classNamespace, string tableName, string storedProcPrefix, string storedProcType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + "Task<" + classNamespace + ".Model." + _ModelGenPath + tableName + "> " + storedProcPrefix + tableName + storedProcType + "(");
            return sb;
        }

        private StringBuilder AddInterfaceMethodStartVoid(string classNamespace, string tableName, string storedProcPrefix, string storedProcType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + "Task " + storedProcPrefix + tableName + storedProcType + "(");
            return sb;
        }

        private StringBuilder AddInterfaceMethodStartList(string classNamespace, string tableName, string storedProcPrefix, string storedProcType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + "Task<List<" + classNamespace + ".Model." + _ModelGenPath + tableName + ">> " + storedProcPrefix + tableName + storedProcType + "(");
            return sb;
        }

        private StringBuilder AddInterfaceMethodParams(string columnType, string paramName)
        {
            StringBuilder sb = new StringBuilder();
            if (paramName != "retMsg")
            {
                sb.Append(columnType + " " + paramName + ", ");
            }
            return sb;
        }

        private StringBuilder AddInterfaceMethodNullableParams(string columnType, string paramName)
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

        private StringBuilder AddInterfaceMethodNullableAndNormalParams(string columnType, string paramName)
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

        private StringBuilder AddInterfaceMethodParamModel(string classNamespace, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(classNamespace + ".Model." + _ModelGenPath + tableName + " " + Shared.Utils.GeneratorUtils.CamelCase(tableName));
            return sb;
        }

        private StringBuilder AddInterfaceFooter()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + "}");
            sb.Append(_NewLine + "}");
            return sb;
        }
    }
}
