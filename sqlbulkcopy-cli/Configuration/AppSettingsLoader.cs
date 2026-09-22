using Microsoft.Extensions.Configuration;

namespace sqlbulkcopy_cli.Configuration;

internal static class AppSettingsLoader
{
    private const string DefaultConfigFile = "sqlbulkcopy.json";

    public static AppSettings Load(string? configPath)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(configPath)
            ? Path.Combine(Environment.CurrentDirectory, DefaultConfigFile)
            : Path.GetFullPath(configPath, Environment.CurrentDirectory);

        var configFileWasExplicit = !string.IsNullOrWhiteSpace(configPath);
        if (configFileWasExplicit && !File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"Configuration file '{resolvedPath}' was not found.", resolvedPath);
        }

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(resolvedPath, optional: !configFileWasExplicit)
            .Build();

        var settings = LoadFromConfiguration(configuration);
        ApplyEnvironmentVariables(settings);
        return settings;
    }

    private static AppSettings LoadFromConfiguration(IConfiguration configuration)
    {
        var map = configuration.GetSection("map").Get<string[]>()?.Where(value => !string.IsNullOrWhiteSpace(value)).ToList() ?? [];

        return new AppSettings
        {
            SourceConnection = GetString(configuration, "sourceConnection"),
            SourceQuery = GetString(configuration, "sourceQuery"),
            SourceObject = GetString(configuration, "sourceObject"),
            DestinationConnection = GetString(configuration, "destinationConnection"),
            DestinationTable = GetString(configuration, "destinationTable"),
            Mapping = GetString(configuration, "mapping"),
            Map = map,
            Mode = GetString(configuration, "mode"),
            ConfirmDestructive = GetBool(configuration, "confirmDestructive"),
            BatchSize = GetInt(configuration, "batchSize"),
            Timeout = GetInt(configuration, "timeout"),
            BulkTimeout = GetInt(configuration, "bulkTimeout"),
            NotifyAfter = GetInt(configuration, "notifyAfter"),
            Transaction = GetBool(configuration, "transaction"),
            KeepIdentity = GetBool(configuration, "keepIdentity"),
            KeepNulls = GetBool(configuration, "keepNulls"),
            CheckConstraints = GetBool(configuration, "checkConstraints"),
            TableLock = GetBool(configuration, "tableLock"),
            FireTriggers = GetBool(configuration, "fireTriggers"),
            UseInternalTransaction = GetBool(configuration, "useInternalTransaction")
        };
    }

    private static void ApplyEnvironmentVariables(AppSettings settings)
    {
        settings.SourceConnection = GetEnvironmentVariable("SQLBULKCOPY_SOURCE_CONNECTION") ?? settings.SourceConnection;
        settings.SourceQuery = GetEnvironmentVariable("SQLBULKCOPY_SOURCE_QUERY") ?? settings.SourceQuery;
        settings.SourceObject = GetEnvironmentVariable("SQLBULKCOPY_SOURCE_OBJECT") ?? settings.SourceObject;
        settings.DestinationConnection = GetEnvironmentVariable("SQLBULKCOPY_DESTINATION_CONNECTION") ?? settings.DestinationConnection;
        settings.DestinationTable = GetEnvironmentVariable("SQLBULKCOPY_DESTINATION_TABLE") ?? settings.DestinationTable;
        settings.Mapping = GetEnvironmentVariable("SQLBULKCOPY_MAPPING") ?? settings.Mapping;
        settings.Mode = GetEnvironmentVariable("SQLBULKCOPY_MODE") ?? settings.Mode;
        settings.ConfirmDestructive = GetEnvironmentBool("SQLBULKCOPY_CONFIRM_DESTRUCTIVE") ?? settings.ConfirmDestructive;
        settings.BatchSize = GetEnvironmentInt("SQLBULKCOPY_BATCH_SIZE") ?? settings.BatchSize;
        settings.Timeout = GetEnvironmentInt("SQLBULKCOPY_TIMEOUT") ?? settings.Timeout;
        settings.BulkTimeout = GetEnvironmentInt("SQLBULKCOPY_BULK_TIMEOUT") ?? settings.BulkTimeout;
        settings.NotifyAfter = GetEnvironmentInt("SQLBULKCOPY_NOTIFY_AFTER") ?? settings.NotifyAfter;
        settings.Transaction = GetEnvironmentBool("SQLBULKCOPY_TRANSACTION") ?? settings.Transaction;
        settings.KeepIdentity = GetEnvironmentBool("SQLBULKCOPY_KEEP_IDENTITY") ?? settings.KeepIdentity;
        settings.KeepNulls = GetEnvironmentBool("SQLBULKCOPY_KEEP_NULLS") ?? settings.KeepNulls;
        settings.CheckConstraints = GetEnvironmentBool("SQLBULKCOPY_CHECK_CONSTRAINTS") ?? settings.CheckConstraints;
        settings.TableLock = GetEnvironmentBool("SQLBULKCOPY_TABLE_LOCK") ?? settings.TableLock;
        settings.FireTriggers = GetEnvironmentBool("SQLBULKCOPY_FIRE_TRIGGERS") ?? settings.FireTriggers;
        settings.UseInternalTransaction = GetEnvironmentBool("SQLBULKCOPY_USE_INTERNAL_TRANSACTION") ?? settings.UseInternalTransaction;

        var envMappings = GetEnvironmentVariable("SQLBULKCOPY_MAP");
        if (!string.IsNullOrWhiteSpace(envMappings))
        {
            settings.Map = envMappings
                .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }

    private static string? GetString(IConfiguration configuration, string key) =>
        configuration[key] ?? configuration.GetSection("copy")[key];

    private static bool? GetBool(IConfiguration configuration, string key)
    {
        var value = GetString(configuration, key);
        return value is null ? null : bool.Parse(value);
    }

    private static int? GetInt(IConfiguration configuration, string key)
    {
        var value = GetString(configuration, key);
        return value is null ? null : int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string? GetEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool? GetEnvironmentBool(string name)
    {
        var value = GetEnvironmentVariable(name);
        return value is null ? null : bool.Parse(value);
    }

    private static int? GetEnvironmentInt(string name)
    {
        var value = GetEnvironmentVariable(name);
        return value is null ? null : int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
