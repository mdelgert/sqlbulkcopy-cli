using sqlbulkcopy_cli.Copy;
using sqlbulkcopy_cli.Configuration;
using sqlbulkcopy_cli.Sql;

namespace sqlbulkcopy_cli.Tests;

public sealed class SqlObjectNameTests
{
    [Theory]
    [InlineData("dbo.Customers", "[dbo].[Customers]")]
    [InlineData("sales.Customer_Export", "[sales].[Customer_Export]")]
    public void TryParse_accepts_two_part_simple_identifiers(string value, string expected)
    {
        var success = SqlObjectName.TryParse(value, out var parsed);

        Assert.True(success);
        Assert.Equal(expected, parsed.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("dbo")]
    [InlineData("dbo.Customers.More")]
    [InlineData("dbo].[Customers")]
    [InlineData("dbo.Customers;DROP TABLE dbo.Customers")]
    public void TryParse_rejects_invalid_identifiers(string? value)
    {
        var success = SqlObjectName.TryParse(value, out _);

        Assert.False(success);
    }
}

public sealed class SqlQueryBuilderTests
{
    [Fact]
    public void BuildSourceQuery_uses_query_when_supplied()
    {
        var query = SqlQueryBuilder.BuildSourceQuery("SELECT 1", null);

        Assert.Equal("SELECT 1", query);
    }

    [Fact]
    public void BuildSourceQuery_generates_safe_select_for_source_object()
    {
        SqlObjectName.TryParse("dbo.vwCustomers", out var sourceObject);

        var query = SqlQueryBuilder.BuildSourceQuery(null, sourceObject);

        Assert.Equal("SELECT * FROM [dbo].[vwCustomers]", query);
    }
}

public sealed class ColumnMappingParserTests
{
    [Fact]
    public void Parse_supports_multiple_mappings()
    {
        var mappings = ColumnMappingParser.Parse(["SourceId=DestinationId", "Name=CustomerName"]);

        Assert.Collection(
            mappings,
            mapping =>
            {
                Assert.Equal("SourceId", mapping.SourceColumn);
                Assert.Equal("DestinationId", mapping.DestinationColumn);
            },
            mapping =>
            {
                Assert.Equal("Name", mapping.SourceColumn);
                Assert.Equal("CustomerName", mapping.DestinationColumn);
            });
    }

    [Fact]
    public void Parse_rejects_invalid_mapping()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ColumnMappingParser.Parse(["broken"]));

        Assert.Contains("Expected source=destination", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_rejects_duplicate_mappings()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ColumnMappingParser.Parse(["SourceId=DestinationId", "SourceId=DestinationName"]));

        Assert.Contains("Duplicate mapping", exception.Message, StringComparison.Ordinal);
    }
}

public sealed class CopyCommandValidatorTests
{
    [Fact]
    public void Validate_requires_exactly_one_source_shape()
    {
        var options = CreateOptions(sourceQuery: "SELECT 1", sourceObject: "dbo.ViewName");

        var exception = Assert.Throws<InvalidOperationException>(() => CopyCommandValidator.Validate(options, isInteractive: true));

        Assert.Contains("Specify either --source-query or --source-object", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_rejects_destructive_mode_without_confirmation()
    {
        var options = CreateOptions(mode: DestinationMode.Truncate);

        var exception = Assert.Throws<InvalidOperationException>(() => CopyCommandValidator.Validate(options, isInteractive: true));

        Assert.Contains("--confirm-destructive", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_rejects_conflicting_transaction_modes()
    {
        var options = CreateOptions(transaction: true, useInternalTransaction: true);

        var exception = Assert.Throws<InvalidOperationException>(() => CopyCommandValidator.Validate(options, isInteractive: false));

        Assert.Contains("cannot be used together", exception.Message, StringComparison.Ordinal);
    }

    private static CopyCommandOptions CreateOptions(
        string? sourceQuery = null,
        string? sourceObject = "dbo.SourceView",
        DestinationMode mode = DestinationMode.Append,
        bool transaction = false,
        bool useInternalTransaction = false) =>
        new(
            SourceConnection: "Server=source;Database=db;Trusted_Connection=True;",
            SourceQuery: sourceQuery,
            SourceObject: sourceObject,
            DestinationConnection: "Server=destination;Database=db;Trusted_Connection=True;",
            DestinationTable: "dbo.TargetTable",
            Mapping: "auto",
            Maps: [],
            Mode: mode,
            ConfirmDestructive: false,
            BatchSize: 10_000,
            Timeout: 30,
            BulkTimeout: 0,
            NotifyAfter: 10_000,
            Transaction: transaction,
            KeepIdentity: false,
            KeepNulls: false,
            CheckConstraints: false,
            TableLock: false,
            FireTriggers: false,
            UseInternalTransaction: useInternalTransaction);
}

public sealed class AppSettingsLoaderTests
{
    [Fact]
    public void Load_applies_environment_variables_over_json_configuration()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        var configPath = Path.Combine(tempDirectory.FullName, "sqlbulkcopy.json");
        File.WriteAllText(
            configPath,
            """
            {
              "sourceConnection": "Server=file-source;Database=FileDb;",
              "destinationConnection": "Server=file-destination;Database=FileDb;",
              "destinationTable": "dbo.Target",
              "batchSize": 5000,
              "map": [ "FileId=Id" ]
            }
            """);

        Environment.SetEnvironmentVariable("SQLBULKCOPY_SOURCE_CONNECTION", "Server=env-source;Database=EnvDb;");
        Environment.SetEnvironmentVariable("SQLBULKCOPY_BATCH_SIZE", "9000");
        Environment.SetEnvironmentVariable("SQLBULKCOPY_MAP", "EnvId=Id;EnvName=Name");

        try
        {
            var settings = AppSettingsLoader.Load(configPath);

            Assert.Equal("Server=env-source;Database=EnvDb;", settings.SourceConnection);
            Assert.Equal("Server=file-destination;Database=FileDb;", settings.DestinationConnection);
            Assert.Equal("dbo.Target", settings.DestinationTable);
            Assert.Equal(9000, settings.BatchSize);
            Assert.Equal(["EnvId=Id", "EnvName=Name"], settings.Map);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SQLBULKCOPY_SOURCE_CONNECTION", null);
            Environment.SetEnvironmentVariable("SQLBULKCOPY_BATCH_SIZE", null);
            Environment.SetEnvironmentVariable("SQLBULKCOPY_MAP", null);
            tempDirectory.Delete(recursive: true);
        }
    }
}
