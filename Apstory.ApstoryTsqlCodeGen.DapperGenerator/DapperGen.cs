using Apstory.ApstoryTsqlCodeGen.Shared.Interfaces;
using Apstory.ApstoryTsqlCodeGen.Shared.Service;
using Apstory.ApstoryTsqlCodeGen.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apstory.ApstoryTsqlCodeGen.DapperGenerator
{
    public class DapperGen : BaseGenerator
    {
        public DapperGen(ISqlTablesRepository tableRepository, string genPath, string modelGenPath, string genPathNamespace, bool threaded)
            : base(tableRepository, genPath, modelGenPath, genPathNamespace, threaded)
        { }

        public async Task Run(string path, string classNamespace, string schema)
        {
            try
            {
                var tables = await _TableRepository.GetDBTables(schema);
                var tablesWithIndexes = await _TableRepository.GetDBTablesWithIndexes(schema);

                GenerateBaseRepository(path, classNamespace);

                if (_Threaded)
                {
                    Parallel.ForEach(tables, (table) =>
                    {
                        GenerateDapperTable(table, path, classNamespace, schema).Wait();
                    });

                    Parallel.ForEach(tablesWithIndexes, (tableWithIndex) =>
                    {
                        GenerateDomainIndexedTable(tableWithIndex, path, classNamespace, schema).Wait();
                    });
                }
                else
                {
                    foreach (var table in tables)
                        await GenerateDapperTable(table, path, classNamespace, schema);

                    foreach (var tableWithIndex in tablesWithIndexes)
                        await GenerateDomainIndexedTable(tableWithIndex, path, classNamespace, schema);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDapperTable(SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                bool addSchemaPath = (schema != "dbo");

                var sb = new StringBuilder();
                sb.Append(AddHeader(classNamespace, table.TABLE_NAME, schema));
                GenerateDapperInsUpd(sb, table, path, classNamespace, schema);
                await GenerateDapperGetById(sb, table, path, classNamespace, schema);
                await GenerateDapperGetByNameIds(sb, table, path, classNamespace, schema);
                await GenerateDapperGetByIds(sb, table, path, classNamespace, schema);
                await GenerateDapperGetByIdsPaging(sb, table, path, classNamespace, schema);
                await GenerateDapperDelHrd(sb, table, path, classNamespace, schema);
                await GenerateDapperDelSft(sb, table, path, classNamespace, schema);
                await GenerateDapperGetBySearch(sb, table, path, classNamespace, schema);
                await GenerateDapperGetBySearchFreeText(sb, table, path, classNamespace, schema);
                sb.Append(AddFooter());
                LogOutputLine();
                LogOutput(sb.ToString());
                LogOutputLine();
                string fileName = table.TABLE_NAME + "Repository" + (addSchemaPath ? "." + schema.ToUpper() : string.Empty) + ".Gen.cs";
                string filePath;
                if (_GenPath.Length > 0)
                    filePath = path + (addSchemaPath ? schema.ToUpper() + @"/" : string.Empty) + _GenPath.Replace(".", "") + @"/" + fileName;
                else
                    filePath = path + (addSchemaPath ? schema.ToUpper() + @"/" : string.Empty) + fileName;

                Shared.Utils.GeneratorUtils.WriteToFile(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainIndexedTable(SqlTable table, string path, string classNamespace, string schema)
        {
            bool addSchemaPath = (schema != "dbo"); try
            {


                var sb = new StringBuilder();
                sb.Append(AddHeaderIndex(classNamespace, table.TABLE_NAME, schema));
                await GenerateDapperGetByIndex(sb, table, path, classNamespace, schema);
                sb.Append(AddFooter());
                LogOutputLine();
                LogOutput(sb.ToString());
                LogOutputLine();
                string fileName = table.TABLE_NAME + "Repository.Index.Gen.cs";
                string filePath;
                if (_GenPath.Length > 0)
                    filePath = path + (addSchemaPath ? schema.ToUpper() + @"/" : string.Empty) + _GenPath.Replace(".", "") + @"/" + fileName;
                else
                    filePath = path + (addSchemaPath ? schema.ToUpper() + @"/" : string.Empty) + fileName;

                Shared.Utils.GeneratorUtils.WriteToFile(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }


        private void GenerateBaseRepository(string path, string classNamespace)
        {
            LogOutputLine("Generate BaseRepository");
            StringBuilder sb = new StringBuilder();

            sb.Append("using System.Data.SqlClient;" + _NewLine + _NewLine +
            "namespace " + classNamespace + ".Dal.Dapper" + _GenPathNamespace + _NewLine +
            "{" + _NewLine +
             _Tab + "public partial class BaseRepository" + _NewLine +
             _Tab + "{" + _NewLine +
             _Tab + _Tab + "private string _ConnectionString;" + _NewLine + _NewLine +
             _Tab + _Tab + "public BaseRepository(string connectionString)" + _NewLine +
             _Tab + _Tab + "{" + _NewLine +
             _Tab + _Tab + _Tab + "_ConnectionString = connectionString;" + _NewLine +
             _Tab + _Tab + "}" + _NewLine +
             _Tab + _Tab + "protected SqlConnection GetConnection()" + _NewLine +
             _Tab + _Tab + "{" + _NewLine +
             _Tab + _Tab + _Tab + "return new SqlConnection(_ConnectionString);" + _NewLine +
             _Tab + _Tab + "}" + _NewLine +
             _Tab + "}" + _NewLine +
             "}");

            LogOutputLine(sb.ToString());

            string fileName = "BaseRepository.Gen.cs";
            string filePath = path + fileName;
            Shared.Utils.GeneratorUtils.WriteToFile(filePath, sb.ToString());
        }


        private async Task GenerateDapperInsUpd(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process dapper InsUpd for table " + table.TABLE_NAME);
                sb.Append(AddMethodStartModel(classNamespace, table.TABLE_NAME, "InsUpd", ""));
                sb.Append(AddMethodParamModel(classNamespace, table.TABLE_NAME));
                sb.Append(")" + _NewLine);
                sb.Append(_Tab + _Tab + "{" + _NewLine);
                sb.Append(_Tab + _Tab + _Tab + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + " ret" + table.TABLE_NAME + " = new " + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + "();" + _NewLine);
                sb.Append(await AddDapperDynamicParamsModel(classNamespace, table.TABLE_NAME, "zgen", "InsUpd", true, schema));
                sb.Append(_Tab + _Tab + _Tab + "string retMsg = dParams.Get<string>(\"RetMsg\");" + _NewLine);
                sb.Append(_Tab + _Tab + _Tab + "int retVal = dParams.Get<int>(\"RetVal\");" + _NewLine);
                sb.Append(_Tab + _Tab + _Tab + "if (retVal == 1) { throw new Exception(retMsg); }" + _NewLine);
                sb.Append(_Tab + _Tab + _Tab + "return ret" + table.TABLE_NAME + ";" + _NewLine);
                sb.Append(_Tab + _Tab + "}" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDapperGetById(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process dapper GetById for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetById", schema);
                sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ById"));
                foreach (var item in spParams)
                {
                    sb.Append(AddMethodNullableParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(")" + _NewLine);
                sb.Append(_Tab + _Tab + "{" + _NewLine);
                sb.Append(_Tab + _Tab + _Tab + "List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + "> ret" + table.TABLE_NAME + " = new List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + ">();" + _NewLine);
                sb.Append(await AddDapperDynamicParams(classNamespace, table.TABLE_NAME, "zgen", "GetById", false, schema));
                sb.Append(_Tab + _Tab + _Tab + "return ret" + table.TABLE_NAME + ";" + _NewLine);
                sb.Append(_Tab + _Tab + "}" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDapperGetByNameIds(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process dapper GetByNameIds for table " + table.TABLE_NAME);
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
                sb.Append(_Tab + _Tab + _Tab + "List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + "> ret" + table.TABLE_NAME + " = new List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + ">();" + _NewLine);
                sb.Append(await AddDapperDynamicParams(classNamespace, table.TABLE_NAME, "zgen", $"GetBy{table.TABLE_NAME}Ids", false, schema));
                sb.Append(_Tab + _Tab + _Tab + "return ret" + table.TABLE_NAME + ";" + _NewLine);
                sb.Append(_Tab + _Tab + "}" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDapperGetByIds(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process dapper GetByIds for table " + table.TABLE_NAME);
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
                    sb.Append(_Tab + _Tab + _Tab + "List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + "> ret" + table.TABLE_NAME + " = new List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + ">();" + _NewLine);
                    sb.Append(await AddDapperDynamicParams(classNamespace, table.TABLE_NAME, "zgen", "GetByIds", false, schema));
                    sb.Append(_Tab + _Tab + _Tab + "return ret" + table.TABLE_NAME + ";" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDapperGetByIdsPaging(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process dapper GetByIds for table " + table.TABLE_NAME);
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
                    sb.Append(_Tab + _Tab + _Tab + "List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + "> ret" + table.TABLE_NAME + " = new List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + ">();" + _NewLine);
                    sb.Append(await AddDapperDynamicParams(classNamespace, table.TABLE_NAME, "zgen", "GetByIdsPaging", false, schema));
                    sb.Append(_Tab + _Tab + _Tab + "return ret" + table.TABLE_NAME + ";" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDapperGetBySearch(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process dapper GetBySearch for table " + table.TABLE_NAME);
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
                    sb.Append(_Tab + _Tab + _Tab + "List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + "> ret" + table.TABLE_NAME + " = new List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + ">();" + _NewLine);
                    sb.Append(await AddDapperDynamicParams(classNamespace, table.TABLE_NAME, "zgen", "GetBySearch", false, schema));
                    sb.Append(_Tab + _Tab + _Tab + "return ret" + table.TABLE_NAME + ";" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDapperGetBySearchFreeText(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process dapper GetBySearchFreeText for table " + table.TABLE_NAME);
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
                    sb.Append(_Tab + _Tab + _Tab + "List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + "> ret" + table.TABLE_NAME + " = new List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + ">();" + _NewLine);
                    sb.Append(await AddDapperDynamicParams(classNamespace, table.TABLE_NAME, "zgen", "GetBySearchFreeText", false, schema));
                    sb.Append(_Tab + _Tab + _Tab + "return ret" + table.TABLE_NAME + ";" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDapperGetByIndex(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process dapper GetByIndex for table " + table.TABLE_NAME);
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
                    sb.Append(_Tab + _Tab + _Tab + "List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + "> ret" + table.TABLE_NAME + " = new List<" + classNamespace + ".Model." + _ModelGenPath + table.TABLE_NAME + ">();" + _NewLine);
                    sb.Append(await AddDapperDynamicParams(classNamespace, table.TABLE_NAME, "zgen", "GetBy" + table.COLUMN_NAME, false, schema));
                    sb.Append(_Tab + _Tab + _Tab + "return ret" + table.TABLE_NAME + ";" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDapperDelHrd(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process dapper DelHrd for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "DelHrd", schema);
                sb.Append(AddMethodStartVoid(classNamespace, table.TABLE_NAME, "Del", "Hrd"));
                foreach (var item in spParams)
                {
                    sb.Append(AddMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(")" + _NewLine);
                sb.Append(_Tab + _Tab + "{" + _NewLine);
                sb.Append(await AddDapperDynamicParamsVoid(classNamespace, table.TABLE_NAME, "zgen", "DelHrd", true, schema));
                sb.Append(_Tab + _Tab + _Tab + "string retMsg = dParams.Get<string>(\"RetMsg\");" + _NewLine);
                sb.Append(_Tab + _Tab + _Tab + "int retVal = dParams.Get<int>(\"RetVal\");" + _NewLine);
                sb.Append(_Tab + _Tab + _Tab + "if (retVal == 1) { throw new Exception(retMsg); }" + _NewLine);
                sb.Append(_Tab + _Tab + "}" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDapperDelSft(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process dapper DelSft for table " + table.TABLE_NAME);
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
                    sb.Append(await AddDapperDynamicParamsVoid(classNamespace, table.TABLE_NAME, "zgen", "DelSft", true, schema));
                    sb.Append(_Tab + _Tab + _Tab + "string retMsg = dParams.Get<string>(\"RetMsg\");" + _NewLine);
                    sb.Append(_Tab + _Tab + _Tab + "int retVal = dParams.Get<int>(\"RetVal\");" + _NewLine);
                    sb.Append(_Tab + _Tab + _Tab + "if (retVal == 1) { throw new Exception(retMsg); }" + _NewLine);
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private StringBuilder AddHeader(string classNamespace, string tableName, string schema)
        {
            var addSchema = schema != "dbo";

            StringBuilder sb = new StringBuilder();
            sb.Append("using System;" + _NewLine +
                "using Dapper;" + _NewLine +
                "using System.Data;" + _NewLine +
                "using System.Data.SqlClient;" + _NewLine +
                "using " + classNamespace + ".Dal.Interface" + (addSchema ? "." + schema.ToUpper() : "") + _GenPathNamespace + ";" + _NewLine +
                "using System.Collections.Generic;" + _NewLine +
                "using System.Threading.Tasks;" + _NewLine +
                "using " + classNamespace + ".Common.Util;" + _NewLine + _NewLine +
                "namespace " + classNamespace + ".Dal.Dapper" + (addSchema ? "." + schema.ToUpper() : "") + _GenPathNamespace + _NewLine +
                "{" + _NewLine +
                _Tab + "public partial class " + tableName + "Repository : BaseRepository, I" + tableName + "Repository" + _NewLine +
                _Tab + "{" + _NewLine +
                _Tab + _Tab + "public " + tableName + "Repository(string connectionString) : base(connectionString) { }" + _NewLine
                );
            return sb;
        }

        private StringBuilder AddHeaderIndex(string classNamespace, string tableName, string schema)
        {
            var addSchema = schema != "dbo";

            StringBuilder sb = new StringBuilder();
            sb.Append("using System;" + _NewLine +
                "using Dapper;" + _NewLine +
                "using System.Data;" + _NewLine +
                "using System.Data.SqlClient;" + _NewLine +
                "using " + classNamespace + ".Dal.Interface" + (addSchema ? "." + schema.ToUpper() : "") + _GenPathNamespace + ";" + _NewLine +
                "using System.Collections.Generic;" + _NewLine +
                "using System.Threading.Tasks;" + _NewLine + _NewLine +
                "namespace " + classNamespace + ".Dal.Dapper" + (addSchema ? "." + schema.ToUpper() : string.Empty) + _GenPathNamespace + _NewLine +
                "{" + _NewLine +
                _Tab + "public partial class " + tableName + "Repository : BaseRepository, I" + tableName + "Repository" + _NewLine +
                _Tab + "{" + _NewLine
                );
            return sb;
        }

        private StringBuilder AddMethodStartModel(string classNamespace, string tableName, string storedProcPrefix, string storedProcType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + "public async Task<" + classNamespace + ".Model." + _ModelGenPath + tableName + "> " + storedProcPrefix + tableName + storedProcType + "(");
            return sb;
        }

        private StringBuilder AddMethodStartVoid(string classNamespace, string tableName, string storedProcPrefix, string storedProcType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + "public async Task " + storedProcPrefix + tableName + storedProcType + "(");
            return sb;
        }

        private StringBuilder AddMethodStartList(string classNamespace, string tableName, string storedProcPrefix, string storedProcType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + "public async Task<List<" + classNamespace + ".Model." + _ModelGenPath + tableName + ">> " + storedProcPrefix + tableName + storedProcType + "(");
            return sb;
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

        private StringBuilder AddMethodNullableParams(string columnType, string paramName)
        {
            StringBuilder sb = new StringBuilder();
            if (paramName != "retMsg")
            {
                if (columnType == "string" || columnType == "Microsoft.SqlServer.Types.SqlGeography")
                {
                    if(paramName.ToLower() == "sortdirection")
                    {
                        sb.Append(columnType + " " + paramName + " = \"ASC\", ");
                    } else
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

        private StringBuilder AddMethodParamModel(string classNamespace, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(classNamespace + ".Model." + _ModelGenPath + tableName + " " + Shared.Utils.GeneratorUtils.CamelCase(tableName));
            return sb;
        }

        private async Task<StringBuilder> AddDapperDynamicParamsModel(string classNamespace, string tableName, string storedProcPrefix, string storedProcType, bool addRetVal, string schema)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + _Tab + "DynamicParameters dParams = new DynamicParameters();" + _NewLine);
            var spParams = await _TableRepository.GetStoredProcParams(storedProcPrefix, tableName, storedProcType, schema);
            if (spParams.Count > 0)
            {
                foreach (var spParam in spParams)
                {
                    if (spParam.TypeName.ToLower() == "geography")
                    {
                        sb.Append(AddDapperDynamicParamModelGeography(spParam.ParameterName.Remove(0, 1), tableName));
                    }
                    else
                    {
                        sb.Append(AddDapperDynamicParamModel(spParam.ParameterName.Remove(0, 1), tableName));
                    }
                }
            }
            if (addRetVal)
            {
                sb.Append(AddDapperDynamicParamsRetVal());
            }
            sb.Append(_Tab + _Tab + _Tab + "using (SqlConnection connection = GetConnection())" + _NewLine);
            sb.Append(_Tab + _Tab + _Tab + "{" + _NewLine);
            sb.Append(_Tab + _Tab + _Tab + _Tab + "ret" + tableName + " = await connection.QueryFirstOrDefaultAsync<" + classNamespace + ".Model." + _ModelGenPath + tableName + ">(\"" + schema + "." + storedProcPrefix + "_" + tableName + "_" + storedProcType + "\", dParams, commandType: System.Data.CommandType.StoredProcedure);" + _NewLine);
            sb.Append(_Tab + _Tab + _Tab + "}" + _NewLine);
            return sb;
        }

        private async Task<StringBuilder> AddDapperDynamicParamsVoid(string classNamespace, string tableName, string storedProcPrefix, string storedProcType, bool addRetVal, string schema)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + _Tab + "DynamicParameters dParams = new DynamicParameters();" + _NewLine);
            var spParams = await _TableRepository.GetStoredProcParams(storedProcPrefix, tableName, storedProcType, schema);
            if (spParams.Count > 0)
            {
                foreach (var spParam in spParams)
                {
                    sb.Append(AddDapperDynamicParam(spParam.ParameterName.Remove(0, 1), tableName, schema));
                }
            }
            if (addRetVal)
            {
                sb.Append(AddDapperDynamicParamsRetVal());
            }
            sb.Append(_Tab + _Tab + _Tab + "using (SqlConnection connection = GetConnection())" + _NewLine);
            sb.Append(_Tab + _Tab + _Tab + "{" + _NewLine);
            sb.Append(_Tab + _Tab + _Tab + _Tab + "await connection.ExecuteAsync(\"" + schema + "." + storedProcPrefix + "_" + tableName + "_" + storedProcType + "\", dParams, commandType: System.Data.CommandType.StoredProcedure);" + _NewLine);
            sb.Append(_Tab + _Tab + _Tab + "}" + _NewLine);
            return sb;
        }

        private async Task<StringBuilder> AddDapperDynamicParams(string classNamespace, string tableName, string storedProcPrefix, string storedProcType, bool addRetVal, string schema)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + _Tab + "DynamicParameters dParams = new DynamicParameters();" + _NewLine);
            var spParams = await _TableRepository.GetStoredProcParams(storedProcPrefix, tableName, storedProcType, schema);

            if (spParams.Count > 0)
            {
                foreach (var spParam in spParams)
                {
                    if (spParam.TypeName.ToLower() == "geography")
                    {
                        sb.Append(AddDapperDynamicParamGeography(spParam.ParameterName.Remove(0, 1), tableName));
                    }
                    else
                    {
                        sb.Append(AddDapperDynamicParam(spParam.ParameterName.Remove(0, 1), tableName, schema));
                    }
                }
            }
            if (addRetVal)
            {
                sb.Append(AddDapperDynamicParamsRetVal());
            }
            sb.Append(_Tab + _Tab + _Tab + "using (SqlConnection connection = GetConnection())" + _NewLine);
            sb.Append(_Tab + _Tab + _Tab + "{" + _NewLine);
            sb.Append(_Tab + _Tab + _Tab + _Tab + "ret" + tableName + " = (await connection.QueryAsync<" + classNamespace + ".Model." + _ModelGenPath + tableName + ">(\"" + schema + "." + storedProcPrefix + "_" + tableName + "_" + storedProcType + "\", dParams, commandType: System.Data.CommandType.StoredProcedure)).AsList();" + _NewLine);
            sb.Append(_Tab + _Tab + _Tab + "}" + _NewLine);
            return sb;
        }

        private StringBuilder AddDapperDynamicParamModel(string paramName, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            if (paramName == "RetMsg")
            {
                sb.Append(_Tab + _Tab + _Tab + "dParams.Add(\"RetMsg\", string.Empty, dbType: DbType.String, direction: ParameterDirection.Output);" + _NewLine);
            }
            else
            {
                sb.Append(_Tab + _Tab + _Tab + "dParams.Add(\"" + paramName + "\", " + Shared.Utils.GeneratorUtils.CamelCase(tableName) + "." + paramName + ");" + _NewLine);
            }
            return sb;
        }

        private StringBuilder AddDapperDynamicParamModelGeography(string paramName, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + _Tab + "dParams.Add(\"" + paramName + "\", $\"POINT({" + Shared.Utils.GeneratorUtils.CamelCase(tableName) + "." + paramName + ".Long.ToString().Replace(\",\",\".\")} {" + Shared.Utils.GeneratorUtils.CamelCase(tableName) + "." + paramName + ".Lat.ToString().Replace(\",\",\".\")} 4326)\");" + _NewLine);
            return sb;
        }

        private StringBuilder AddDapperDynamicParamGeography(string paramName, string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + _Tab + "dParams.Add(\"" + paramName + "\", $\"POINT({" + Shared.Utils.GeneratorUtils.CamelCase(paramName) + ".Long.ToString().Replace(\",\",\".\")} {" + Shared.Utils.GeneratorUtils.CamelCase(paramName) + ".Lat.ToString().Replace(\",\",\".\")} 4326)\");" + _NewLine);
            return sb;
        }

        private StringBuilder AddDapperDynamicParam(string paramName, string tableName, string schema)
        {
            StringBuilder sb = new StringBuilder();
            if (paramName == "RetMsg")
            {
                sb.Append(_Tab + _Tab + _Tab + "dParams.Add(\"RetMsg\", string.Empty, dbType: DbType.String, direction: ParameterDirection.Output);" + _NewLine);
            }
            else
            {
                var paramValue = Shared.Utils.GeneratorUtils.CamelCase(paramName);
                if (paramName == "Uniqueidentifiers")
                    paramValue += ".ToDataTable().AsTableValuedParameter(\"" + schema + ".udtt_Uniqueidentifiers\")";

                if (paramName == "Ids")
                    paramValue += ".ToDataTable().AsTableValuedParameter(\"" + schema + ".udtt_Ints\")";

                sb.Append(_Tab + _Tab + _Tab + "dParams.Add(\"" + paramName + "\", " + paramValue + ");" + _NewLine);
            }
            return sb;
        }

        private StringBuilder AddDapperDynamicParamsRetVal()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_Tab + _Tab + _Tab + "dParams.Add(\"RetVal\", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);" + _NewLine);
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
