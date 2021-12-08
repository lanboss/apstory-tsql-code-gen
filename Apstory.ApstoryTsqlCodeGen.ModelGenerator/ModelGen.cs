using Apstory.ApstoryTsqlCodeGen.Shared.Interfaces;
using Apstory.ApstoryTsqlCodeGen.Shared.Service;
using Apstory.ApstoryTsqlCodeGen.Shared.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Apstory.ApstoryTsqlCodeGen.ModelGenerator
{
    public class ModelGen : BaseGenerator
    {
        public ModelGen(ISqlTablesRepository tableRepository, string genPath, string modelGenPath, string genPathNamespace, bool threaded)
    : base(tableRepository, genPath, modelGenPath, genPathNamespace, threaded)
        { }

        public async Task Run(string path, string classNamespace, string schema, bool convertLongsToString, bool includeForeignKeys)
        {
            var tables = await _TableRepository.GetDBTables(schema);
            var tablesWithIndexes = await _TableRepository.GetDBTablesWithIndexes(schema);

            try
            {
                if (_Threaded)
                {
                    Parallel.ForEach(tables, (table) =>
                    {
                        GenerateModelTable(table, path, classNamespace, schema, convertLongsToString, includeForeignKeys).Wait();
                    });
                }
                else
                {
                    foreach (var table in tables)
                        await GenerateModelTable(table, path, classNamespace, schema, convertLongsToString, includeForeignKeys);

                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }

        }

        private async Task GenerateModelTable(SqlTable table, string path, string classNamespace, string schema, bool convertLongsToString, bool includeForeignKeys)
        {
            try
            {
                var sb = new StringBuilder();
                string model = await _TableRepository.GetTableModel(table.TABLE_NAME, schema, convertLongsToString, includeForeignKeys);

                var addExtraImports = model.Contains("[JsonConverter(typeof(LongToStringConverter))]");
                sb = AddModelClassHeader(classNamespace, schema, addExtraImports);

                //Bytes are used for enum purposes
                if (includeForeignKeys && model.Contains(" byte "))
                    sb.Insert(0, $"using {classNamespace}.Model.Enum;" + _NewLine);

                sb.Append(model);

                if (sb.ToString().Contains("Geography"))
                {
                    sb.Insert(0, "using Microsoft.SqlServer.Types;" + _NewLine);
                }
                sb.Append(AddModelClassFooter());
                LogOutput(sb.ToString());
                LogOutputLine();
                bool addSchemaPath = (schema != "dbo");

                string fileName = table.TABLE_NAME + (addSchemaPath ? "." + schema.ToUpper() : string.Empty) + ".Gen.cs";
                string filePath;

                if (_GenPath.Length > 0)
                    filePath = path + (addSchemaPath ? schema.ToUpper() + @"\" : string.Empty) + _GenPath.Replace(".", "") + "\\" + fileName;
                else
                    filePath = path + (addSchemaPath ? schema.ToUpper() + @"\" : string.Empty) + fileName;

                Shared.Utils.GeneratorUtils.WriteToFile(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private StringBuilder AddModelClassHeader(string classNamespace, string schema, bool addJsonConvertImports)
        {
            var addSchema = schema != "dbo";


            StringBuilder sb = new StringBuilder();

            if (addJsonConvertImports)
            {
                sb.Append($"using {classNamespace}.Common;{_NewLine}");
                sb.Append($"using Newtonsoft.Json;{_NewLine}");
            }

            sb.Append("using System;" + _NewLine + _NewLine +
                "namespace " + classNamespace + ".Model" + (addSchema ? "." + schema.ToUpper() : string.Empty) + _GenPath + _NewLine +
                "{" + _NewLine);
            return sb;
        }

        private StringBuilder AddModelClassFooter()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_NewLine + "}");
            return sb;
        }
    }
}
