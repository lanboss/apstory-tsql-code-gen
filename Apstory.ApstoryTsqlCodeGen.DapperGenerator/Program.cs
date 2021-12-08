using Apstory.ApstoryTsqlCodeGen.Shared.Repositories;
using Apstory.ApstoryTsqlCodeGen.Shared.Models;
using Apstory.ApstoryTsqlCodeGen.Shared.Repositories;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Apstory.ApstoryTsqlCodeGen.DapperGenerator
{
    class Program
    {
        private static string _GenPath = ""; // ".Gen"
        private static string _ModelGenPath = ""; // "Gen."
        private static string _GenPathNamespace = ".Gen";

        static void Main(string[] args)
        {
            CommandLineApplication commandLineApplication =
              new CommandLineApplication(throwOnUnexpectedArg: false);

            CommandOption path = commandLineApplication.Option(
              "-p |--path <Path>", "Path to create and save generated model class files", CommandOptionType.SingleValue);

            CommandOption classNamespace = commandLineApplication.Option(
              "-n |--classNamespace <ClassNamespace>", "Class namespace", CommandOptionType.SingleValue);

            CommandOption conString = commandLineApplication.Option(
              "-c |--constring <ConnectionString>", "Database connection string", CommandOptionType.MultipleValue);

            CommandOption genPath = commandLineApplication.Option(
              "-g |--GenPath <GenPath>", "Add gen path true or false", CommandOptionType.SingleValue);

            CommandOption genPathNamespace = commandLineApplication.Option(
              "-gn |--GenPathNamespace <GenPathNamespace>", "Add gen path true or false", CommandOptionType.SingleValue);

            CommandOption schemaString = commandLineApplication.Option(
              "-schema |--schemaString <SchemaString>", "Schema (Defaults to 'dbo')", CommandOptionType.MultipleValue);

            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.OnExecute(() =>
            {
                var schema = "dbo";
                if (schemaString.HasValue())
                    schema = schemaString.Value();

                Console.WriteLine($"Path : {path.Value()}");
                Console.WriteLine($"ClassNamepsace : {classNamespace.Value()}");
                Console.WriteLine($"ConnectionString : {conString.Value()}");
                Console.WriteLine($"SchemaString: {schema}");

                var schemaAdd = schema != "dbo";
                _ModelGenPath = (schemaAdd ? schema.ToUpper() + "." : "");
                if (path.HasValue() && classNamespace.HasValue() && conString.HasValue())
                {
                    if (genPath.HasValue())
                    {
                        if (genPath.Value() == "true")
                        {
                            _GenPath = ".Gen";
                            if (genPathNamespace.HasValue())
                            {
                                if (genPathNamespace.Value() == "false")
                                {
                                    _GenPathNamespace = "";
                                }
                                else
                                {
                                    _ModelGenPath += "Gen.";
                                }
                            }
                            else
                            {
                                _ModelGenPath += "Gen.";
                            }
                        }
                    }
                    ProcessDapperGenerator(path.Value(), classNamespace.Value(), conString.Value(), schema);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR");
                    Console.WriteLine($"Path and connection string is required");
                    Console.ForegroundColor = ConsoleColor.White;
                }

                return 0;
            });
            commandLineApplication.Execute(args);
        }

        private static void ProcessDapperGenerator(string path, string classNamespace, string conString, string schema)
        {
            var tablesRepository = new CachedSqlTablesRepository(conString);
            new DapperGen(tablesRepository, _GenPath, _ModelGenPath, _GenPathNamespace, false).Run(path, classNamespace, schema).Wait();
        }
    }
}
