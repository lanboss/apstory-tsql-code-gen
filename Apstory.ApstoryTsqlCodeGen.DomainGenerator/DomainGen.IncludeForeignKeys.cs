using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apstory.ApstoryTsqlCodeGen.Shared.Interfaces;
using Apstory.ApstoryTsqlCodeGen.Shared.Service;
using Apstory.ApstoryTsqlCodeGen.Shared.Models;
using System.IO;

namespace Apstory.ApstoryTsqlCodeGen.DomainGenerator
{
    public partial class DomainGen : BaseGenerator
    {
        private async Task GenerateDomainTableIncludeForeignKeys(SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                bool addSchema = (schema != "dbo");

                var spParams = await _TableRepository.GetTableColumnsByTableName(table.TABLE_NAME, schema);
                var foreignParams = spParams.Where(s => !string.IsNullOrWhiteSpace(s.ForeignTable)).ToList();


                if (!foreignParams.Any())
                    return;

                var sb = new StringBuilder();
                sb.Append(AddHeaderIncludeForeignKeys(classNamespace, table.TABLE_NAME, schema, foreignParams));
                await GenerateDomainGetByIdIncludeForeignKeys(sb, table, path, classNamespace, schema, foreignParams);
                await GenerateDomainGetByIdsIncludeForeignKeys(sb, table, path, classNamespace, schema, foreignParams);
                await GenerateDomainGetByIdsPagingIncludeForeignKeys(sb, table, path, classNamespace, schema, foreignParams);
                await GenerateDomainGetByNameIdsIncludeForeignKeys(sb, table, path, classNamespace, schema, foreignParams);
                await GenerateDomainGetBySearchIncludeForeignKeys(sb, table, path, classNamespace, schema, foreignParams);
                await GenerateDomainGetBySearchFreeTextIncludeForeignKeys(sb, table, path, classNamespace, schema, foreignParams);
                await GenerateDomainAppendHelperMethods(sb, table, path, classNamespace, schema, foreignParams);

                sb.Append(AddFooter());
                LogOutputLine();
                LogOutput(sb.ToString());
                LogOutputLine();
                string fileName = table.TABLE_NAME + "Service" + (addSchema ? "." + schema.ToUpper() : string.Empty) + ".Foreign.Gen.cs";
                string filePath;
                if (_GenPath.Length > 0)
                    filePath = Path.Join(path, (addSchema ? schema.ToUpper() : string.Empty), _GenPath.Replace(".", ""), fileName);
                else
                    filePath = Path.Join(path, (addSchema ? schema.ToUpper() : string.Empty), fileName);
                Shared.Utils.GeneratorUtils.WriteToFile(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainAppendHelperMethods(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema, List<SqlTableColumn> foreignParams)
        {
            try
            {
                sb.Append($"{_NewLine}");
                foreach (var foreign in foreignParams)
                {
                    var friendlyName = foreign.ColumnName.Substring(0, foreign.ColumnName.Length - 2);
                    var friendlyTableName = Shared.Utils.GeneratorUtils.CamelCase(table.TABLE_NAME);

                    sb.Append($"{_Tab}{_Tab}protected async Task<List<{classNamespace}.Model.{_ModelGenPath}{table.TABLE_NAME}>> Append{friendlyName}(List<{classNamespace}.Model.{_ModelGenPath}{table.TABLE_NAME}> {friendlyTableName}s){_NewLine}");
                    sb.Append($"{_Tab}{_Tab}{{{_NewLine}");


                    //if (foreign.ColumnType == "byte")
                    //{
                    //    sb.Append($"{_Tab}{_Tab}{_Tab}foreach(var {friendlyTableName} in {friendlyTableName}s){_NewLine}");
                    //    sb.Append($"{_Tab}{_Tab}{_Tab}{{{_NewLine}");
                    //    sb.Append($"{_Tab}{_Tab}{_Tab}{_Tab}{friendlyTableName}.{friendlyName} = new {friendlyName}() {{ {friendlyName} = {friendlyTableName}.{foreign.ColumnName}, {friendlyName} = (({foreign.ColumnName}){friendlyTableName}.{foreign.ColumnName}).ToString().Replace(\"_\", \" \") }}");
                    //    sb.Append($"{_Tab}{_Tab}{_Tab}}}{_NewLine}");
                    //}
                    //else
                    //{
                    //Determine Distinct Ids:

                    if (foreign.ColumnType == "byte")
                    {
                        sb.Append($"{_Tab}{_Tab}{_Tab}var distinct{friendlyName}s = await _{Shared.Utils.GeneratorUtils.CamelCase(foreign.ForeignTable)}Repo.Get{foreign.ForeignTable}ById(null, null);{_NewLine}{_NewLine}");
                    }
                    else
                    {
                        if (foreign.IsNullable)
                            sb.Append($"{_Tab}{_Tab}{_Tab}var distinct{foreign.ColumnName}s = {friendlyTableName}s.Where(s => s.{foreign.ColumnName}.HasValue).Select(s => s.{foreign.ColumnName}.Value).Distinct().ToList();{_NewLine}");
                        else
                            sb.Append($"{_Tab}{_Tab}{_Tab}var distinct{foreign.ColumnName}s = {friendlyTableName}s.Select(s => s.{foreign.ColumnName}).Distinct().ToList();{_NewLine}");

                        //Get all foreign entries:

                        sb.Append($"{_Tab}{_Tab}{_Tab}var distinct{friendlyName}s = await _{Shared.Utils.GeneratorUtils.CamelCase(foreign.ForeignTable)}Repo.Get{foreign.ForeignTable}By{foreign.ForeignTable}Ids(distinct{friendlyName}Ids, null);{_NewLine}{_NewLine}");
                    }


                    //Assign all foreign entries:
                    sb.Append($"{_Tab}{_Tab}{_Tab}foreach(var {friendlyTableName} in {friendlyTableName}s){_NewLine}");
                    sb.Append($"{_Tab}{_Tab}{_Tab}{{{_NewLine}");
                    sb.Append($"{_Tab}{_Tab}{_Tab}{_Tab}{friendlyTableName}.{friendlyName} = distinct{friendlyName}s.FirstOrDefault(s => s.{foreign.ForeignTable}Id == {friendlyTableName}.{foreign.ColumnName});{_NewLine}");
                    sb.Append($"{_Tab}{_Tab}{_Tab}}}{_NewLine}");
                    //}

                    sb.Append($"{_Tab}{_Tab}{_NewLine}");
                    sb.Append($"{_Tab}{_Tab}{_Tab}return {friendlyTableName}s;{_NewLine}");
                    sb.Append($"{_Tab}{_Tab}}}{_NewLine}");
                    sb.Append($"{_Tab}{_Tab}{_NewLine}");
                }


            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }


        private StringBuilder AddHeaderIncludeForeignKeys(string classNamespace, string tableName, string schema, List<SqlTableColumn> foreignParams)
        {
            bool addSchema = (schema != "dbo");
            StringBuilder sb = new StringBuilder();
            sb.Append($"using System;{_NewLine}");
            sb.Append($"using {classNamespace}.Dal.Interface{(addSchema ? "." + schema.ToUpper() : "")}{_GenPathNamespace};{_NewLine}");
            sb.Append($"using {classNamespace}.Domain.Interface{(addSchema ? "." + schema.ToUpper() : "")}{_GenPathNamespace};{_NewLine}");
            sb.Append($"using System.Collections.Generic;{_NewLine}");
            sb.Append($"using System.Threading.Tasks;{_NewLine}");
            sb.Append($"using System.Linq;{_NewLine}{_NewLine}");


            sb.Append($"namespace {classNamespace}.Domain{(addSchema ? "." + schema.ToUpper() : "")}{_GenPathNamespace}{_NewLine}");
            sb.Append($"{{{_NewLine}");
            sb.Append($"{_Tab}public partial class {tableName}Service : I{tableName}Service{_NewLine}");
            sb.Append($"{_Tab}{{{_NewLine}");
            // sb.Append($"{_Tab}{_Tab}private readonly I{tableName}Repository _repo;{_NewLine}");

            var hasForeignRepos = foreignParams.Any();
            if (hasForeignRepos)
            {
                foreach (var foreignTableName in foreignParams.Select(s => s.ForeignTable).Distinct())
                    sb.Append($"{_Tab}{_Tab}private readonly I{foreignTableName}Repository _{Shared.Utils.GeneratorUtils.CamelCase(foreignTableName)}Repo;{_NewLine}");

                sb.Append($"{_NewLine}");

                sb.Append($"{_Tab}{_Tab}public {tableName}Service(I{tableName}Repository repo");
                foreach (var foreignTableName in foreignParams.Select(s => s.ForeignTable).Distinct())
                {
                    sb.Append($", I{foreignTableName}Repository {Shared.Utils.GeneratorUtils.CamelCase(foreignTableName)}Repo");
                }

                sb.Append($"){_NewLine}");

                sb.Append($"{_Tab}{_Tab}{{{_NewLine}");
                sb.Append($"{_Tab}{_Tab}{_Tab}_repo = repo;{_NewLine}");
                foreach (var foreignTableName in foreignParams.Select(s => s.ForeignTable).Distinct())
                    sb.Append($"{_Tab}{_Tab}{_Tab}_{Shared.Utils.GeneratorUtils.CamelCase(foreignTableName)}Repo = {Shared.Utils.GeneratorUtils.CamelCase(foreignTableName)}Repo;{_NewLine}");

                sb.Append($"{_Tab}{_Tab}}}{_NewLine}");
            }

            // sb.Append($"{_Tab}}}");
            return sb;
        }

        private async Task GenerateDomainIndexedTableIncludeForeignKeys(SqlTable tableWithIndex, string path, string classNamespace, string schema)
        {
            try
            {
                bool addSchema = (schema != "dbo");

                var spParams = await _TableRepository.GetTableColumnsByTableName(tableWithIndex.TABLE_NAME, schema);
                var foreignParams = spParams.Where(s => !string.IsNullOrWhiteSpace(s.ForeignTable)).ToList();

                if (!foreignParams.Any())
                    return;

                var sb = new StringBuilder();
                sb.Append(AddHeaderIndex(classNamespace, tableWithIndex.TABLE_NAME, schema));
                await GenerateDomainGetByIndexIncludeForeignKeys(sb, tableWithIndex, path, classNamespace, schema, foreignParams);
                sb.Append(AddFooter());
                LogOutputLine();
                LogOutput(sb.ToString());
                LogOutputLine();
                string fileName = tableWithIndex.TABLE_NAME + "Service" + (addSchema ? "." + schema.ToUpper() : string.Empty) + ".ForeignIndex.Gen.cs";
                string filePath;
                if (_GenPath.Length > 0)
                    filePath = Path.Join(path, (addSchema ? schema.ToUpper() : string.Empty), _GenPath.Replace(".", ""), fileName);
                else
                    filePath = Path.Join(path, (addSchema ? schema.ToUpper() : string.Empty), fileName);
                Shared.Utils.GeneratorUtils.WriteToFile(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetByIdIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema, List<SqlTableColumn> foreignParams)
        {
            try
            {
                LogOutputLine("Process domain GetByIdIncludeForeignKeys for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetById", schema);
                sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ByIdIncludeForeignKeys"));
                foreach (var item in spParams)
                {
                    sb.Append(AddMethodNullableParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(")" + _NewLine);
                sb.Append(_Tab + _Tab + "{" + _NewLine);
                sb.Append($"{_Tab}{_Tab}{_Tab}var ret{table.TABLE_NAME} = await _repo.Get{table.TABLE_NAME}ById(");
                foreach (var item in spParams)
                {
                    sb.Append(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1)) + ", ");
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(");" + _NewLine);

                foreach (var foreign in foreignParams)
                {
                    var friendlyName = foreign.ColumnName.Substring(0, foreign.ColumnName.Length - 2);
                    sb.Append($"{_Tab}{_Tab}{_Tab}await Append{friendlyName}(ret{table.TABLE_NAME});{_NewLine}");
                }

                sb.Append($"{_Tab}{_Tab}{_Tab}return ret{table.TABLE_NAME};{_NewLine}");
                sb.Append(_Tab + _Tab + "}" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetByNameIdsIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema, List<SqlTableColumn> foreignParams)
        {
            try
            {
                LogOutputLine("Process domain GetByNameIdsIncludeForeignKeys for table " + table.TABLE_NAME);
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

                sb.Append($"{_Tab}{_Tab}public async Task<List<{classNamespace}.Model.{_ModelGenPath}{table.TABLE_NAME}>> Get{table.TABLE_NAME}By{table.TABLE_NAME}IdsIncludeForeignKeys(");
                foreach (var item in spParams)
                {
                    sb.Append(AddMethodNullableAndNormalParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(")" + _NewLine);
                sb.Append(_Tab + _Tab + "{" + _NewLine);
                sb.Append($"{_Tab}{_Tab}{_Tab}var ret{table.TABLE_NAME} = await _repo.Get{table.TABLE_NAME}By{table.TABLE_NAME}Ids(");
                foreach (var item in spParams)
                {
                    sb.Append(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1)) + ", ");
                }
                sb.Remove(sb.Length - 2, 2);
                sb.Append(");" + _NewLine);

                foreach (var foreign in foreignParams)
                {
                    var friendlyName = foreign.ColumnName.Substring(0, foreign.ColumnName.Length - 2);
                    sb.Append($"{_Tab}{_Tab}{_Tab}await Append{friendlyName}(ret{table.TABLE_NAME});{_NewLine}");
                }

                sb.Append($"{_Tab}{_Tab}{_Tab}return ret{table.TABLE_NAME};{_NewLine}");
                sb.Append(_Tab + _Tab + "}" + _NewLine);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetByIdsIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema, List<SqlTableColumn> foreignParams)
        {
            try
            {
                LogOutputLine();
                LogOutputLine();
                LogOutputLine("Process domain GetByIdsIncludeForeignKeys for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetByIds", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ByIdsIncludeForeignKeys"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodNullableParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append($"{_Tab}{_Tab}{_Tab}var ret{table.TABLE_NAME} = await _repo.Get{table.TABLE_NAME}ByIds(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);

                    foreach (var foreign in foreignParams)
                    {
                        var friendlyName = foreign.ColumnName.Substring(0, foreign.ColumnName.Length - 2);
                        sb.Append($"{_Tab}{_Tab}{_Tab}await Append{friendlyName}(ret{table.TABLE_NAME});{_NewLine}");
                    }

                    sb.Append($"{_Tab}{_Tab}{_Tab}return ret{table.TABLE_NAME};{_NewLine}");
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetByIdsPagingIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema, List<SqlTableColumn> foreignParams)
        {
            try
            {
                LogOutputLine("Process domain GetByIdsPagingIncludeForeignKeys for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetByIdsPaging", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ByIdsPagingIncludeForeignKeys"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodNullableParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append($"{_Tab}{_Tab}{_Tab}var ret{table.TABLE_NAME} = await _repo.Get{table.TABLE_NAME}ByIdsPaging(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);

                    foreach (var foreign in foreignParams)
                    {
                        var friendlyName = foreign.ColumnName.Substring(0, foreign.ColumnName.Length - 2);
                        sb.Append($"{_Tab}{_Tab}{_Tab}await Append{friendlyName}(ret{table.TABLE_NAME});{_NewLine}");
                    }

                    sb.Append($"{_Tab}{_Tab}{_Tab}return ret{table.TABLE_NAME};{_NewLine}");
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetBySearchIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema, List<SqlTableColumn> foreignParams)
        {
            try
            {
                LogOutputLine("Process domain GetBySearchIncludeForeignKeys for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBySearch", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "BySearchIncludeForeignKeys"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append($"{_Tab}{_Tab}{_Tab}var ret{table.TABLE_NAME} = await _repo.Get{table.TABLE_NAME}BySearch(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);

                    foreach (var foreign in foreignParams)
                    {
                        var friendlyName = foreign.ColumnName.Substring(0, foreign.ColumnName.Length - 2);
                        sb.Append($"{_Tab}{_Tab}{_Tab}await Append{friendlyName}(ret{table.TABLE_NAME});{_NewLine}");
                    }

                    sb.Append($"{_Tab}{_Tab}{_Tab}return ret{table.TABLE_NAME};{_NewLine}");
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetBySearchFreeTextIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema, List<SqlTableColumn> foreignParams)
        {
            try
            {
                LogOutputLine("Process domain GetBySearchFreeTextIncludeForeignKeys for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBySearchFreeText", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "BySearchFreeTextIncludeForeignKeys"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append($"{_Tab}{_Tab}{_Tab}var ret{table.TABLE_NAME} = await _repo.Get{table.TABLE_NAME}BySearchFreeText(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);

                    foreach (var foreign in foreignParams)
                    {
                        var friendlyName = foreign.ColumnName.Substring(0, foreign.ColumnName.Length - 2);
                        sb.Append($"{_Tab}{_Tab}{_Tab}await Append{friendlyName}(ret{table.TABLE_NAME});{_NewLine}");
                    }

                    sb.Append($"{_Tab}{_Tab}{_Tab}return ret{table.TABLE_NAME};{_NewLine}");
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateDomainGetByIndexIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema, List<SqlTableColumn> foreignParams)
        {
            try
            {
                LogOutputLine("Process domain GetByIndexIncludeForeignKeys for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBy" + table.COLUMN_NAME, schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddMethodStartList(classNamespace, table.TABLE_NAME, "Get", "By" + table.COLUMN_NAME + "IncludeForeignKeys"));
                    foreach (var item in spParams)
                    {
                        sb.Append(AddMethodParams(item.ColumnType, Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")" + _NewLine);
                    sb.Append(_Tab + _Tab + "{" + _NewLine);
                    sb.Append($"{_Tab}{_Tab}{_Tab}var ret{table.TABLE_NAME} = await _repo.Get{table.TABLE_NAME}By{table.COLUMN_NAME}(");
                    foreach (var item in spParams)
                    {
                        sb.Append(AddRepoMethodParams(Shared.Utils.GeneratorUtils.CamelCase(item.ParameterName.Remove(0, 1))));
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(");" + _NewLine);

                    foreach (var foreign in foreignParams)
                    {
                        var friendlyName = foreign.ColumnName.Substring(0, foreign.ColumnName.Length - 2);
                        sb.Append($"{_Tab}{_Tab}{_Tab}await Append{friendlyName}(ret{table.TABLE_NAME});{_NewLine}");
                    }

                    sb.Append($"{_Tab}{_Tab}{_Tab}return ret{table.TABLE_NAME};{_NewLine}");
                    sb.Append(_Tab + _Tab + "}" + _NewLine);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

    }
}
