// See top-level statements https://aka.ms/new-console-template
// See System.CommandLinedocs: https://docs.microsoft.com/en-us/dotnet/standard/commandline/

using Apstory.ApstoryTsqlCodeGen.ServiceCollectionExtensionGenerator;
using Apstory.ApstoryTsqlCodeGen.Shared.Repositories;
using System.CommandLine;

static void PrintErrorMessage(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(message);
    Console.ResetColor();
}

var outputOption = new Option<string>("--output", "Path to write output files");
var topLevelNamespaceOption = new Option<string>("--namespace", "Top level namespace to use when generating files");
var connectionStringOption = new Option<string>("--connection-string", "DB connection string");
var schemaOption = new Option<string>("--schema", getDefaultValue: () => "dbo", "Schema of database tables");

var rootCommand = new RootCommand("CodeGen for adding services and repositories via dependency injection");
rootCommand.AddOption(outputOption);
rootCommand.AddOption(topLevelNamespaceOption);
rootCommand.AddOption(connectionStringOption);
rootCommand.AddOption(schemaOption);

rootCommand.SetHandler(
    (
        outputOptionValue,
        topLevelNamespaceOptionValue,
        connectionStringOptionValue,
        schemaOptionValue
    ) =>
    {
        return CodeGenerator(outputOptionValue, topLevelNamespaceOptionValue, connectionStringOptionValue, schemaOptionValue);
    },
    outputOption,
    topLevelNamespaceOption,
    connectionStringOption,
    schemaOption);

return await rootCommand.InvokeAsync(args);

async Task<int> CodeGenerator(string output, string topLevelNamespace, string connectionString, string schema)
{
    var config = new Configuration(output, topLevelNamespace, connectionString, schema);
    var dbRepository = new CachedSqlTablesRepository(config.ConnectionString);

    var generator = new Generator(config, dbRepository);
    await generator.Run();

    Console.WriteLine("Done");
    return 0;
}
