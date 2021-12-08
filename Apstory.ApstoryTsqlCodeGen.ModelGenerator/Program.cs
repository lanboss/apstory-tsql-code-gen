using Apstory.ApstoryTsqlCodeGen.Shared.Repositories;
using Microsoft.Extensions.CommandLineUtils;
using System;

namespace Apstory.ApstoryTsqlCodeGen.ModelGenerator
{
    class Program
    {
        private static string _GenPath = "";

        static void Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            CommandOption path = commandLineApplication.Option(
              "-p |--path <Path>", "Path to create and save generated model class files", CommandOptionType.SingleValue);

            CommandOption classNamepsace = commandLineApplication.Option(
              "-n |--classNamepsace <ClassNamepsace>", "Class namespace", CommandOptionType.SingleValue);

            CommandOption conString = commandLineApplication.Option(
              "-c |--constring <ConnectionString>", "Database connection string", CommandOptionType.MultipleValue);

            CommandOption genPath = commandLineApplication.Option(
              "-g |--GenPath <GenPath>", "Add gen path true or false", CommandOptionType.SingleValue);

            CommandOption includeForeignKeys = commandLineApplication.Option(
              "-i |--IncludeForeignKeys <true/false>", "Include foreign object mapping methods, true or false", CommandOptionType.SingleValue);

            CommandOption convertLongs = commandLineApplication.Option(
              "-cl |--ConvertLongs <true/false>", "Convert longs to strings", CommandOptionType.SingleValue);

            CommandOption schemaString = commandLineApplication.Option(
              "-schema |--schemaString <SchemaString>", "Schema (Defaults to 'dbo')", CommandOptionType.MultipleValue);

            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.OnExecute(() =>
            {
                var schema = "dbo";
                if (schemaString.HasValue())
                    schema = schemaString.Value();

                bool convertLongsToString = convertLongs.HasValue();
                bool includeForeignObjects = includeForeignKeys.HasValue();

                Console.WriteLine($"Path : {path.Value()}");
                Console.WriteLine($"ClassNamepsace : {classNamepsace.Value()}");
                Console.WriteLine($"ConnectionString : {conString.Value()}");
                Console.WriteLine($"SchemaString: {schema}");
                Console.WriteLine($"ConvertLongsToString: {convertLongsToString}");
                Console.WriteLine($"IncludeForeignKeys: {includeForeignObjects}");

                if (path.HasValue() && classNamepsace.HasValue() && conString.HasValue())
                {
                    if (genPath.HasValue())
                    {
                        if (genPath.Value() == "true")
                        {
                            _GenPath = ".Gen";
                        }
                    }

                    ProcessModelGenerator(path.Value(), classNamepsace.Value(), conString.Value(), schema, convertLongsToString, includeForeignObjects);
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

            //ProcessModelGenerator(@"c:\testgen\",
            //    @"Apstory.Ledger.Model",
            //    "Data Source=qa.apstory.co.za;Initial Catalog=Apstory.Ledger;User ID=testadmin;Password=@Test@Admin2014");
        }

        private static void ProcessModelGenerator(string path, string classNamespace, string conString, string schema, bool convertLongsToString, bool includeForeignKeys)
        {
            var tablesRepository = new CachedSqlTablesRepository(conString);
            new ModelGen(tablesRepository, _GenPath, "", "", false).Run(path, classNamespace, schema, convertLongsToString, includeForeignKeys).Wait();
        }

    }
}
