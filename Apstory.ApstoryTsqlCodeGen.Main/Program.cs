using Apstory.ApstoryTsqlCodeGen.DapperGenerator;
using Apstory.ApstoryTsqlCodeGen.DomainGenerator;
using Apstory.ApstoryTsqlCodeGen.InterfaceGenerator;
using Apstory.ApstoryTsqlCodeGen.ModelGenerator;
using Apstory.ApstoryTsqlCodeGen.Shared.Repositories;
using Apstory.ApstoryTsqlCodeGen.Shared.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Apstory.Common.Tsql.Main
{
    class Program
    {

        static void Main(string[] args)
        {

            CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);

            CommandOption classNamespace = commandLineApplication.Option(
              "--classNamespace <c>", "Class namespace", CommandOptionType.SingleValue);
            //<test1>
            CommandOption conString = commandLineApplication.Option(
              "--constring <con>", "Database connection string", CommandOptionType.MultipleValue);
            //Server=.\\sqlexpress08;[UserID]=sa;password=chcontrol;Database=EAIDB
            CommandOption convertLongs = commandLineApplication.Option(
              "--ConvertLongs <change>", "Convert longs to strings", CommandOptionType.SingleValue);
            //true
            CommandOption includeForeignKeys = commandLineApplication.Option(
              "--IncludeForeignKeys <foreign>", "Include foreign object mapping methods, true or false", CommandOptionType.SingleValue);
            //true
            CommandOption genPathStringList = commandLineApplication.Option(
              "--GenPathList <domain>", "Add a list of paths you want to be Generated spearated by commas in a string  i.e. 'domain,model,dalDapper,interfaceDomain,interfaceDal'", CommandOptionType.MultipleValue);
            //domain,model,dalDapper,interfaceDomain,interfaceDal
            CommandOption genPathNamespaceStringList = commandLineApplication.Option(
              "--GenPathNamespaceList <domain>", "Add a list of paths you want to be Generated spearated by commas in a string i.e. 'domain,model,dalDapper,interfaceDomain,interfaceDal'", CommandOptionType.MultipleValue);
            //domain,model,dalDapper,interfaceDomain,interfaceDal
            CommandOption schemaString = commandLineApplication.Option(
              "--schemaString <s>", "Schema (Defaults to 'dbo')", CommandOptionType.MultipleValue);

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
            if ( args.Length==0)
                args = new[] { "--constring:Server=.\\sqlexpress;User ID=sa;password=x;Database=ApstoryDB", "--classNamespace:test1" };
            //               args = new[] { "--constring:Server=.\\sqlexpress;User ID=sa;password=x;Database=test1", "--classNamespace:test1" };
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
            var domainTask = domainGen.Run(Path.Join("Domain", $"{classNamespace}.Domain"), classNamespace, schema, includeForeignKeys);

            var modelGen = new ModelGen(tablesRepository,
                GeneratorUtils.GenPathString(genPathList, "model"),
                GeneratorUtils.GenPathModelString(genPathList, genPathNamespaceList, "model", schema),
                GeneratorUtils.GenPathNamespaceString(genPathNamespaceList, "model"),
                true);
            var modelTask = modelGen.Run(Path.Join("Model", $"{classNamespace}.Model"), classNamespace, schema, convertLongsToString, includeForeignKeys);

            var dapperGen = new DapperGen(tablesRepository,
                GeneratorUtils.GenPathString(genPathList, "dalDapper"),
                GeneratorUtils.GenPathModelString(genPathList, genPathNamespaceList, "dalDapper", schema),
                GeneratorUtils.GenPathNamespaceString(genPathNamespaceList, "dalDapper"),
                true);
            var dapperTask = dapperGen.Run(Path.Join("Dal", $"{classNamespace}.Dal.Dapper"), classNamespace, schema);

            var interfaceDomainGen = new InterfaceGen(tablesRepository,
                GeneratorUtils.GenPathString(genPathList, "interfaceDomain"),
                GeneratorUtils.GenPathModelString(genPathList, genPathNamespaceList, "interfaceDomain", schema),
                GeneratorUtils.GenPathNamespaceString(genPathNamespaceList, "interfaceDomain"),
                true);
            var interfaceDomainTask = interfaceDomainGen.Run(Path.Join("Domain", $"{classNamespace}.Domain.Interface"), "Service", classNamespace, schema, includeForeignKeys);

            var interfaceDalGen = new InterfaceGen(tablesRepository,
                GeneratorUtils.GenPathString(genPathList, "interfaceDal"),
                GeneratorUtils.GenPathModelString(genPathList, genPathNamespaceList, "interfaceDal", schema),
                GeneratorUtils.GenPathNamespaceString(genPathNamespaceList, "interfaceDal"),
                true);
            var interfaceDalTask = interfaceDalGen.Run(Path.Join("Dal", $"{classNamespace}.Dal.Interface"), "Repository", classNamespace, schema, false);
            Task.WaitAll(domainTask, dapperTask, interfaceDalTask, interfaceDomainTask, interfaceDalTask, modelTask);

            sw.Stop();
            Console.WriteLine($"Done {sw.ElapsedMilliseconds}ms");
        }
    }
}
