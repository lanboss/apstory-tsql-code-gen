using Apstory.ApstoryTsqlCodeGen.Shared.Interfaces;
using Apstory.ApstoryTsqlCodeGen.Shared.Service;
using Apstory.ApstoryTsqlCodeGen.Shared.Models;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apstory.ApstoryTsqlCodeGen.InterfaceGenerator
{
    public partial class InterfaceGen : BaseGenerator
    {
        private async Task GenerateInterfaceTableIncludeForeignKeys(SqlTable table, string path, string type, string classNamespace, string schema)
        {
            try
            {
                bool addSchema = (schema != "dbo");

                var spParams = await _TableRepository.GetTableColumnsByTableName(table.TABLE_NAME, schema);
                var foreignParams = spParams.Where(s => s.ColumnId > 1 && s.ColumnName.EndsWith("Id")).ToList();

                if (!foreignParams.Any())
                    return;

                var sb = new StringBuilder();
                sb.Append(AddInterfaceHeader(classNamespace, table.TABLE_NAME, type, schema));
                await GenerateInterfaceGetByIdIncludeForeignKeys(sb, table, path, classNamespace, schema);
                await GenerateInterfaceGetByIdsIncludeForeignKeys(sb, table, path, classNamespace, schema);
                await GenerateInterfaceGetByIdsPagingIncludeForeignKeys(sb, table, path, classNamespace, schema);
                await GenerateInterfaceGetByNameIdsIncludeForeignKeys(sb, table, path, classNamespace, schema);
                await GenerateInterfaceGetBySearchIncludeForeignKeys(sb, table, path, classNamespace, schema);
                await GenerateInterfaceGetBySearchFreeTextIncludeForeignKeys(sb, table, path, classNamespace, schema);
                sb.Append(AddInterfaceFooter());
                LogOutputLine();
                LogOutput(sb.ToString());
                LogOutputLine();
                string fileName = "I" + table.TABLE_NAME + type + (addSchema ? "." + schema.ToUpper() : "") + ".Foreign.Gen.cs";
                string filePath;
                if (_GenPath.Length > 0)
                    filePath = path + (addSchema ? schema.ToUpper() + @"/" : string.Empty) + _GenPath.Replace(".", "") + "//" + fileName;
                else
                    filePath = path + (addSchema ? schema.ToUpper() + @"/" : string.Empty) + fileName;

                Shared.Utils.GeneratorUtils.WriteToFile(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }


        private async Task GenerateInterfaceIndexedTableIncludeForeignKeys(SqlTable tableWithIndex, string path, string type, string classNamespace, string schema)
        {
            try
            {
                bool addSchema = (schema != "dbo");

                var spParams = await _TableRepository.GetTableColumnsByTableName(tableWithIndex.TABLE_NAME, schema);
                var foreignParams = spParams.Where(s => s.ColumnId > 1 && s.ColumnName.EndsWith("Id")).ToList();

                if (!foreignParams.Any())
                    return;

                var sb = new StringBuilder();
                sb.Append(AddInterfaceHeader(classNamespace, tableWithIndex.TABLE_NAME, type, schema));
                await GenerateInterfaceGetByIndexIncludeForeignKeys(sb, tableWithIndex, path, classNamespace, schema);
                sb.Append(AddInterfaceFooter());
                LogOutputLine();
                LogOutput(sb.ToString());
                LogOutputLine();
                string fileName = "I" + tableWithIndex.TABLE_NAME + type + (addSchema ? "." + schema.ToUpper() : "") + ".ForeignIndex.Gen.cs";
                string filePath;
                if (_GenPath.Length > 0)
                    filePath = path + (addSchema ? schema.ToUpper() + @"/" : string.Empty) + _GenPath.Replace(".", "") + @"/" + fileName;
                else
                    filePath = path + (addSchema ? schema.ToUpper() + @"/" : string.Empty) + fileName;

                Shared.Utils.GeneratorUtils.WriteToFile(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async Task GenerateInterfaceGetByIdIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetById for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetById", schema);
                sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ByIdIncludeForeignKeys"));
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

        private async Task GenerateInterfaceGetByNameIdsIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
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

                sb.Append($"{_Tab}{_Tab}Task<List<{classNamespace}.Model.{_ModelGenPath}{table.TABLE_NAME}>> Get{table.TABLE_NAME}By{table.TABLE_NAME}IdsIncludeForeignKeys(");
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

        private async Task GenerateInterfaceGetByIdsIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetByIds for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetByIds", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ByIdsIncludeForeignKeys"));
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

        private async Task GenerateInterfaceGetByIdsPagingIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetByIdsPaging for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetByIdsPaging", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "ByIdsPagingIncludeForeignKeys"));
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

        private async Task GenerateInterfaceGetBySearchIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetBySearch for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBySearch", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "BySearchIncludeForeignKeys"));
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

        private async Task GenerateInterfaceGetBySearchFreeTextIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetBySearchFreeText for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBySearchFreeText", schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "BySearchFreeTextIncludeForeignKeys"));
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

        private async Task GenerateInterfaceGetByIndexIncludeForeignKeys(StringBuilder sb, SqlTable table, string path, string classNamespace, string schema)
        {
            try
            {
                LogOutputLine("Process interface GetByIndex for table " + table.TABLE_NAME);
                var spParams = await _TableRepository.GetStoredProcParams("zgen", table.TABLE_NAME, "GetBy" + table.COLUMN_NAME, schema);
                if (spParams.Count > 0)
                {
                    sb.Append(AddInterfaceMethodStartList(classNamespace, table.TABLE_NAME, "Get", "By" + table.COLUMN_NAME + "IncludeForeignKeys"));
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
    }
}
