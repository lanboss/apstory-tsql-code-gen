namespace Apstory.ApstoryTsqlCodeGen.ServiceCollectionExtensionGenerator;

internal class Configuration
{
    public Configuration(
        string outputPath,
        string topLevelNamespace,
        string connectionString,
        string schema
    )
    {
        OutputPath = outputPath;
        TopLevelNamespace = topLevelNamespace;
        ConnectionString = connectionString;
        Schema = schema;
    }

    public string OutputPath { get; }
    public string TopLevelNamespace { get; }
    public string ConnectionString { get; }
    public string Schema { get; }
}
