using Apstory.ApstoryTsqlCodeGen.Shared.Interfaces;

namespace Apstory.ApstoryTsqlCodeGen.ServiceCollectionExtensionGenerator;

internal class Generator
{
    private readonly Configuration _configuration;
    private readonly ISqlTablesRepository _tableRepository;

    public Generator(Configuration configuration, ISqlTablesRepository tableRepository)
    {
        _configuration = configuration;
        _tableRepository = tableRepository;
    }

    public async Task Run()
    {
        var tables = await _tableRepository.GetDBTables();

        var repos = AddGeneratedRepositories.Generate(_configuration.TopLevelNamespace, tables.Select(i => i.TABLE_NAME));

        var services = AddGeneratedServices.Generate(_configuration.TopLevelNamespace, tables.Select(i => i.TABLE_NAME));

        var ioTasks = new[] {
            File.WriteAllTextAsync(Path.Combine(_configuration.OutputPath, "AddGeneratedRepositories.cs"), repos, System.Text.Encoding.UTF8),
            File.WriteAllTextAsync(Path.Combine(_configuration.OutputPath, "AddGeneratedServices.cs"), services, System.Text.Encoding.UTF8)
        };

        await Task.WhenAll(ioTasks);
    }
}
