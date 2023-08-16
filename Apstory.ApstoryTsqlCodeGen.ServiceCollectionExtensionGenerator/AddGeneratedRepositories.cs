using System.Reflection;
using System.Text;

namespace Apstory.ApstoryTsqlCodeGen.ServiceCollectionExtensionGenerator;

internal class AddGeneratedRepositories
{
    public static string Generate(string topLevelNamespace, string generatedNamespace, string schema, IEnumerable<string> tableNames)
    {
        /*
        namespace Microsoft.Extensions.DependencyInjection;

        public static class AddGeneratedRepositoriesServiceCollectionExtensions 
        {
            public static IServiceCollection AddGeneratedRepositories(this IServiceCollection services, string connectionString)
            {
                // Alphabetical
                services.AddTransient<IEventFilterTagRepository, EventFilterTagRepository>(x => new EventFilterTagRepository(connectionString));

                return services;
            }
        }
        */

        var version = Assembly.GetExecutingAssembly().GetName();

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine($"/* {version} */");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"using Microsoft.Extensions.DependencyInjection;");
        stringBuilder.AppendLine($"using {topLevelNamespace}.Dal.Interface{(schema != "dbo" ? $".{schema.ToUpper()}" : "")};");
        stringBuilder.AppendLine($"using {topLevelNamespace}.Dal.Dapper{(schema != "dbo" ? $".{schema.ToUpper()}" : "")};");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"namespace {generatedNamespace};");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("public static class AddGeneratedRepositoriesServiceCollectionExtension");
        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine("    public static IServiceCollection AddGeneratedRepositories(this IServiceCollection services, string connectionString)");
        stringBuilder.AppendLine("    {");
        foreach (var tableName in tableNames.OrderBy(i => i))
        {
            stringBuilder.AppendLine($"        services.AddTransient<I{tableName}Repository, {tableName}Repository>(x => new {tableName}Repository(connectionString));");
        }
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("        return services;");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine("}");
        stringBuilder.AppendLine();
        return stringBuilder.ToString();
    }
}
