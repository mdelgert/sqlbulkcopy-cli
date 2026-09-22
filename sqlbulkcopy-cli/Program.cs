using System.CommandLine;
using sqlbulkcopy_cli.Configuration;
using sqlbulkcopy_cli.Console;
using sqlbulkcopy_cli.Copy;
using sqlbulkcopy_cli.Sql;

return await ProgramEntryPoint.RunAsync(args);

internal static class ProgramEntryPoint
{
    public static async Task<int> RunAsync(string[] args)
    {
        var settings = AppSettingsLoader.Load(BootstrapArguments.GetConfigPath(args));
        AzureAuthenticationConfigurator.Configure();

        var command = CopyCommandFactory.Create(settings, new StandardConsole());
        var root = new RootCommand("Stream rows from a SQL Server or Azure SQL query/view/table into a destination table using SqlBulkCopy.");
        root.Add(command);

        return await root.Parse(args).InvokeAsync();
    }
}
