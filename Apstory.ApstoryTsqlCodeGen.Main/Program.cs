using Apstory.ApstoryTsqlCodeGen.DapperGenerator;
using Apstory.ApstoryTsqlCodeGen.DomainGenerator;
using Apstory.ApstoryTsqlCodeGen.InterfaceGenerator;
using Apstory.ApstoryTsqlCodeGen.ModelGenerator;
using Apstory.ApstoryTsqlCodeGen.Shared.Repositories;
using Apstory.ApstoryTsqlCodeGen.Shared.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Apstory.Common.Tsql.Main
{
    class Program
    {

        static void Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            CommandOption classNamespace = commandLineApplication.Option(
              "-n |--classNamespace <ClassNamespace>", "Class namespace", CommandOptionType.SingleValue);

            CommandOption conString = commandLineApplication.Option(
              "-c |--constring <ConnectionString>", "Database connection string", CommandOptionType.MultipleValue);

            CommandOption convertLongs = commandLineApplication.Option(
              "-cl |--ConvertLongs <true/false>", "Convert longs to strings", CommandOptionType.SingleValue);

            CommandOption includeForeignKeys = commandLineApplication.Option(
              "-i |--IncludeForeignKeys <true/false>", "Include foreign object mapping methods, true or false", CommandOptionType.SingleValue);

            CommandOption genPathStringList = commandLineApplication.Option(
              "-gl |--GenPathList <GenPathList>", "Add a list of paths you want to be Generated spearated by commas in a string  i.e. 'domain,model,dalDapper,interfaceDomain,interfaceDal'", CommandOptionType.MultipleValue);

            CommandOption genPathNamespaceStringList = commandLineApplication.Option(
              "-gnl |--GenPathNamespaceList <GenPathNamespaceList>", "Add a list of paths you want to be Generated spearated by commas in a string i.e. 'domain,model,dalDapper,interfaceDomain,interfaceDal'", CommandOptionType.MultipleValue);

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

                Console.WriteLine($"ClassNamepsace : {classNamespace.Value()}");
                Console.WriteLine($"ConnectionString : {conString.Value()}");
                Console.WriteLine($"SchemaString: {schema}");
                Console.WriteLine($"GenPathList: {genPathStringList.Value()}");
                Console.WriteLine($"GenPathNamespaceList: {genPathNamespaceStringList.Value()}");
                Console.WriteLine($"ConvertLongsToString: {convertLongsToString}");
                Console.WriteLine($"IncludeForeignKeys: {includeForeignObjects}");

                var genPathList = genPathStringList.HasValue() ? genPathStringList.Value().Split(",") : new string[] { };
                var genPathNamespaceList = genPathNamespaceStringList.HasValue() ? genPathNamespaceStringList.Value().Split(",") : new string[] { };

                Console.WriteLine($"GenPathList: {genPathList}");
                Console.WriteLine($"GenPathNamespaceList: {genPathNamespaceList}");


                if (classNamespace.HasValue() && conString.HasValue())
                {
                    ProcessGenerator(classNamespace.Value(), conString.Value(), schema, convertLongsToString, genPathList, genPathNamespaceList, includeForeignObjects);
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

        static void ProcessGenerator(string classNamespace, string conString, string schema, bool convertLongsToString, string[] genPathList, string[] genPathNamespaceList, bool includeForeignKeys)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.Write("Warming up cache...");
            var tablesRepository = new CachedSqlTablesRepository(conString);
            tablesRepository.WarmUp("zgen", schema).Wait();
            sw.Stop();
            Console.WriteLine($"Done {sw.ElapsedMilliseconds}ms");
            Console.Write("Generating code...");


            sw.Restart();
            var domainGen = new DomainGen(tablesRepository,
                GeneratorUtils.GenPathString(genPathList, "domain"),
                GeneratorUtils.GenPathModelString(genPathList, genPathNamespaceList, "domain", schema),
                GeneratorUtils.GenPathNamespaceString(genPathNamespaceList, "domain"),
                true);
            var domainTask = domainGen.Run($"Domain/{classNamespace}.Domain/", classNamespace, schema, includeForeignKeys);

            var modelGen = new ModelGen(tablesRepository,
                GeneratorUtils.GenPathString(genPathList, "model"),
                GeneratorUtils.GenPathModelString(genPathList, genPathNamespaceList, "model", schema),
                GeneratorUtils.GenPathNamespaceString(genPathNamespaceList, "model"),
                true);
            var modelTask = modelGen.Run($"Model/{classNamespace}.Model/", classNamespace, schema, convertLongsToString, includeForeignKeys);

            var dapperGen = new DapperGen(tablesRepository,
                GeneratorUtils.GenPathString(genPathList, "dalDapper"),
                GeneratorUtils.GenPathModelString(genPathList, genPathNamespaceList, "dalDapper", schema),
                GeneratorUtils.GenPathNamespaceString(genPathNamespaceList, "dalDapper"),
                true);
            var dapperTask = dapperGen.Run($"Dal/{classNamespace}.Dal.Dapper/", classNamespace, schema);

            var interfaceDomainGen = new InterfaceGen(tablesRepository,
                GeneratorUtils.GenPathString(genPathList, "interfaceDomain"),
                GeneratorUtils.GenPathModelString(genPathList, genPathNamespaceList, "interfaceDomain", schema),
                GeneratorUtils.GenPathNamespaceString(genPathNamespaceList, "interfaceDomain"),
                true);
            var interfaceDomainTask = interfaceDomainGen.Run($"Domain/{classNamespace}.Domain.Interface/", "Service", classNamespace, schema, includeForeignKeys);

            var interfaceDalGen = new InterfaceGen(tablesRepository,
                GeneratorUtils.GenPathString(genPathList, "interfaceDal"),
                GeneratorUtils.GenPathModelString(genPathList, genPathNamespaceList, "interfaceDal", schema),
                GeneratorUtils.GenPathNamespaceString(genPathNamespaceList, "interfaceDal"),
                true);
            var interfaceDalTask = interfaceDalGen.Run($"Dal/{classNamespace}.Dal.Interface/", "Repository", classNamespace, schema, false);
            Task.WaitAll(domainTask, dapperTask, interfaceDalTask, interfaceDomainTask, interfaceDalTask, modelTask);

            sw.Stop();
            Console.WriteLine($"Done {sw.ElapsedMilliseconds}ms");
        }
    }
}
