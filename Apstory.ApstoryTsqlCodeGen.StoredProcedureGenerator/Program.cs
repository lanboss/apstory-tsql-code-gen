using Apstory.ApstoryTsqlCodeGen.Shared.Repositories;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Apstory.ApstoryTsqlCodeGen.StoredProcedureGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLineApplication commandLineApplication =
              new CommandLineApplication(throwOnUnexpectedArg: false);

            CommandOption sourceConString = commandLineApplication.Option(
              "-s |--sourceConString <SourceConnectionString>", "Source database connection string", CommandOptionType.SingleValue);

            CommandOption destConString = commandLineApplication.Option(
              "-d |--destConString <DestConnectionString>", "Destination database connection string", CommandOptionType.MultipleValue);

            CommandOption schemaString = commandLineApplication.Option(
              "-schema |--schemaString <SchemaString>", "Schema (Defaults to 'dbo')", CommandOptionType.MultipleValue);

            commandLineApplication.HelpOption("-? | -h | --help");

            commandLineApplication.OnExecute(() =>
            {
                var schema = "dbo";
                if (schemaString.HasValue())
                    schema = schemaString.Value();

                Console.WriteLine($"SourceConnectionString : {sourceConString.Value()}");
                Console.WriteLine($"DestConnectionString : {destConString.Value()}");
                Console.WriteLine($"SchemaString: {schema}");

                if (sourceConString.HasValue() && destConString.HasValue())
                {
                    ExecuteGeneratorStoredProcs(sourceConString.Value(), destConString.Value(), schema).Wait();
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

        private static async Task ExecuteGeneratorStoredProcs(string sourceConString, string destConString, string schema)
        {
            Console.WriteLine("Creating stored procedures required for CRUD stored procedure generation");
            await CreateGeneratorStoredProcs(sourceConString, destConString);
            Console.WriteLine("Execute CRUD stored procedure generation");
            var tables = new SqlTablesRepository(destConString);
            await tables.ExecuteCreateAllGeneratorStoredProcedures(schema);
        }

        private static async Task CreateGeneratorStoredProcs(string sourceConString, string destConString)
        {
            List<string> createScripts = new List<string>();
            var sourceTables = new SqlTablesRepository(sourceConString);
            var destTables = new SqlTablesRepository(destConString);

            createScripts = await sourceTables.GetGeneratorStoredProcCreateScript();
            foreach (var script in createScripts)
            {
                await destTables.AddGeneratorStoredProcs(ReplaceCreateWithAlter(script));
            }
        }

        private static string ReplaceCreateWithAlter(string script)
        {
            return Regex.Replace(script, @"\b(CREATE)\b\s+\b(PROCEDURE)\b", "CREATE OR ALTER PROCEDURE");
        }
    }
}
