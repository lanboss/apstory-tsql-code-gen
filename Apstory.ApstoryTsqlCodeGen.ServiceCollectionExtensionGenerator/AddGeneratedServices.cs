using System.Reflection;
using System.Text;

namespace Apstory.ApstoryTsqlCodeGen.ServiceCollectionExtensionGenerator;

internal class AddGeneratedServices
{
    public static string Generate(string topLevelNamespace, string generatedNamespace, IEnumerable<string> tableNames)
    {
        /*
        using xxxx;

        namespace Microsoft.Extensions.DependencyInjection;

        public static class AddGeneratedServicesServiceCollectionExtension 
        {
            public static IServiceCollection AddGeneratedServices(this IServiceCollection services)
            {
                // Alphabetical
                services.AddTransient<IEventStatusService, EventStatusService>();

                return services;
            }
        }
        */

        var version = Assembly.GetExecutingAssembly().GetName();

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine($"/* {version} */");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"using Microsoft.Extensions.DependencyInjection;");
        stringBuilder.AppendLine($"using {topLevelNamespace}.Domain.Interface;");
        stringBuilder.AppendLine($"using {topLevelNamespace}.Domain;");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"namespace {generatedNamespace};");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("public static class AddGeneratedServicesServiceCollectionExtension");
        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine("    public static IServiceCollection AddGeneratedServices(this IServiceCollection services)");
        stringBuilder.AppendLine("    {");
        foreach (var tableName in tableNames.OrderBy(i => i))
        {
            stringBuilder.AppendLine($"        services.AddTransient<I{tableName}Service, {tableName}Service>();");
        }
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("        return services;");
        stringBuilder.AppendLine("    }");
        stringBuilder.AppendLine("}");
        stringBuilder.AppendLine();
        return stringBuilder.ToString();
    }
}
