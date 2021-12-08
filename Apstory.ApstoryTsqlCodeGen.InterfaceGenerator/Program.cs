using Apstory.ApstoryTsqlCodeGen.Shared.Repositories;
using Microsoft.Extensions.CommandLineUtils;
using System;

namespace Apstory.ApstoryTsqlCodeGen.InterfaceGenerator
{
    class Program
    {
        private static string _GenPath = ""; // ".Gen"
        private static string _ModelGenPath = ""; // "Gen."
        private static string _GenPathNamespace = ".Gen";

        static void Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            CommandOption path = commandLineApplication.Option(
              "-p |--path <Path>", "Path to create and save generated model class files", CommandOptionType.SingleValue);

            CommandOption classNamespace = commandLineApplication.Option(
              "-n |--classNamespace <ClassNamespace>", "Class namespace", CommandOptionType.SingleValue);

            CommandOption type = commandLineApplication.Option(
              "-t |--type <Type>", "Interface type i.e. repository or service", CommandOptionType.SingleValue);

            CommandOption conString = commandLineApplication.Option(
              "-c |--constring <ConnectionString>", "Database connection string", CommandOptionType.MultipleValue);

            CommandOption includeForeignKeys = commandLineApplication.Option(
              "-i |--IncludeForeignKeys <true/false>", "Include foreign object mapping methods, true or false", CommandOptionType.SingleValue);

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

                bool includeForeignObjects = includeForeignKeys.HasValue();

                Console.WriteLine($"Path : {path.Value()}");
                Console.WriteLine($"ClassNamepsace : {classNamespace.Value()}");
                Console.WriteLine($"Type : {type.Value()}");
                Console.WriteLine($"ConnectionString : {conString.Value()}");
                Console.WriteLine($"SchemaString: {schema}");
                Console.WriteLine($"IncludeForeignKeys: {includeForeignObjects}");

                var schemaAdd = schema != "dbo";
                _ModelGenPath = (schemaAdd ? schema.ToUpper() + "." : "");
                if (path.HasValue() && classNamespace.HasValue() && conString.HasValue() && type.HasValue())
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

                    ProcessInterfaceGenerator(path.Value(), classNamespace.Value(), type.Value(), conString.Value(), schema, includeForeignObjects);
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

        private static void ProcessInterfaceGenerator(string path, string classNamespace, string type, string conString, string schema, bool includeForeignKeys)
        {
            var tablesRepository = new CachedSqlTablesRepository(conString);
            new InterfaceGen(tablesRepository, _GenPath, _ModelGenPath, _GenPathNamespace, false).Run(path, type, classNamespace, schema, includeForeignKeys).Wait();
        }
    }
}
