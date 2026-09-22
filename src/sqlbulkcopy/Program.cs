using System.Diagnostics;
using Microsoft.Data.SqlClient;

string? sourceConnectionString = null;
string? destinationConnectionString = null;
string? query = null;
string? destinationTable = null;
var batchSize = 10_000;
var truncate = false;
var showHelp = false;
string? parseError = null;

for (var i = 0; i < args.Length; i++)
{
    try
    {
        switch (args[i])
        {
            case "--source":
                sourceConnectionString = ReadValue(args, ref i, "--source");
                break;
            case "--destination":
                destinationConnectionString = ReadValue(args, ref i, "--destination");
                break;
            case "--query":
                query = ReadValue(args, ref i, "--query");
                break;
            case "--table":
                destinationTable = ReadValue(args, ref i, "--table");
                break;
            case "--batch-size":
                var batchSizeText = ReadValue(args, ref i, "--batch-size");
                if (!int.TryParse(batchSizeText, out batchSize) || batchSize <= 0)
                {
                    parseError = "--batch-size must be a positive integer.";
                }

                break;
            case "--truncate":
                truncate = true;
                break;
            case "--help":
            case "-h":
                showHelp = true;
                break;
            default:
                parseError = $"Unknown argument: {args[i]}";
                break;
        }
    }
    catch (ArgumentException ex)
    {
        parseError = ex.Message;
    }

    if (parseError is not null)
    {
        break;
    }
}

if (showHelp)
{
    ShowUsage();
    return 0;
}

if (parseError is not null)
{
    Console.Error.WriteLine(parseError);
    Console.Error.WriteLine();
    ShowUsage();
    return 1;
}

sourceConnectionString ??= Environment.GetEnvironmentVariable("SQLCOPY_SOURCE");
destinationConnectionString ??= Environment.GetEnvironmentVariable("SQLCOPY_DESTINATION");

if (string.IsNullOrWhiteSpace(sourceConnectionString))
{
    Console.Error.WriteLine("A source connection string is required. Use --source or SQLCOPY_SOURCE.");
    Console.Error.WriteLine();
    ShowUsage();
    return 1;
}

if (string.IsNullOrWhiteSpace(destinationConnectionString))
{
    Console.Error.WriteLine("A destination connection string is required. Use --destination or SQLCOPY_DESTINATION.");
    Console.Error.WriteLine();
    ShowUsage();
    return 1;
}

if (string.IsNullOrWhiteSpace(query))
{
    Console.Error.WriteLine("--query is required.");
    Console.Error.WriteLine();
    ShowUsage();
    return 1;
}

if (string.IsNullOrWhiteSpace(destinationTable))
{
    Console.Error.WriteLine("--table is required.");
    Console.Error.WriteLine();
    ShowUsage();
    return 1;
}

try
{
    var stopwatch = Stopwatch.StartNew();

    await using var source = new SqlConnection(sourceConnectionString);
    await using var destination = new SqlConnection(destinationConnectionString);

    await source.OpenAsync();
    Console.WriteLine("Source connected.");

    await destination.OpenAsync();
    Console.WriteLine("Destination connected.");

    if (truncate)
    {
        Console.WriteLine($"Truncating {destinationTable}...");

        await using var truncateCommand = new SqlCommand($"TRUNCATE TABLE {destinationTable}", destination)
        {
            CommandTimeout = 0
        };

        await truncateCommand.ExecuteNonQueryAsync();
    }

    await using var command = new SqlCommand(query, source)
    {
        CommandTimeout = 0
    };

    await using var reader = await command.ExecuteReaderAsync();

    long rowsCopied = 0;

    using var bulkCopy = new SqlBulkCopy(destination)
    {
        DestinationTableName = destinationTable,
        BatchSize = batchSize,
        BulkCopyTimeout = 0,
        EnableStreaming = true,
        NotifyAfter = batchSize
    };

    bulkCopy.SqlRowsCopied += (_, e) => rowsCopied = e.RowsCopied;

    for (var i = 0; i < reader.FieldCount; i++)
    {
        var columnName = reader.GetName(i);
        bulkCopy.ColumnMappings.Add(columnName, columnName);
    }

    Console.WriteLine("Copying...");

    await bulkCopy.WriteToServerAsync(reader);

    stopwatch.Stop();

    if (rowsCopied > 0)
    {
        Console.WriteLine($"Copied {rowsCopied:N0} rows in {stopwatch.Elapsed.TotalSeconds:0.0} seconds.");
    }
    else
    {
        Console.WriteLine($"Copy completed in {stopwatch.Elapsed.TotalSeconds:0.0} seconds.");
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static string ReadValue(string[] args, ref int index, string optionName)
{
    if (index + 1 >= args.Length)
    {
        throw new ArgumentException($"Missing value for {optionName}.");
    }

    index++;
    return args[index];
}

static void ShowUsage()
{
    Console.WriteLine(
        """
        Usage:
          sqlbulkcopy --source "<connection>" --query "<select>" --destination "<connection>" --table "schema.Table" [--truncate] [--batch-size 10000]

        Environment variables:
          SQLCOPY_SOURCE
          SQLCOPY_DESTINATION
        """);
}
