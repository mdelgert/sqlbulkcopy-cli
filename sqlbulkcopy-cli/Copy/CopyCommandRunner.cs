using Microsoft.Data.SqlClient;
using sqlbulkcopy_cli.Console;
using sqlbulkcopy_cli.Sql;

namespace sqlbulkcopy_cli.Copy;

internal sealed class CopyCommandRunner
{
    private readonly IConsoleWriter _console;

    public CopyCommandRunner(IConsoleWriter console)
    {
        _console = console;
    }

    public async Task<int> RunAsync(CopyCommandOptions options, CancellationToken cancellationToken)
    {
        CopyCommandValidator.Validate(options, _console.IsInteractive);

        var sourceDescriptor = SqlObjectName.TryParse(options.SourceObject, out var parsedSourceObject)
            ? parsedSourceObject.ToString()
            : options.SourceQuery is null ? "(query not provided)" : "custom query";
        var destinationObject = SqlObjectName.ParseRequired(options.DestinationTable, "destination table");
        var sourceQuery = SqlQueryBuilder.BuildSourceQuery(options.SourceQuery, parsedSourceObject);
        var mappings = ColumnMappingParser.Parse(options.Maps);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await using var sourceConnection = new SqlConnection(options.SourceConnection);
        await using var destinationConnection = new SqlConnection(options.DestinationConnection);

        await sourceConnection.OpenAsync(cancellationToken);
        await destinationConnection.OpenAsync(cancellationToken);

        _console.WriteLine($"Source:      {ConnectionDisplayFormatter.Format(sourceConnection)} / {sourceDescriptor}");
        _console.WriteLine($"Destination: {ConnectionDisplayFormatter.Format(destinationConnection)} / {destinationObject}");
        _console.WriteLine($"Mode:        {options.Mode.ToString().ToLowerInvariant()}");
        _console.WriteLine($"Batch size:  {options.BatchSize:N0}");
        _console.WriteLine();
        _console.WriteLine("Copying...");
        _console.WriteLine();

        await using var sourceCommand = sourceConnection.CreateCommand();
        sourceCommand.CommandText = sourceQuery;
        sourceCommand.CommandTimeout = options.Timeout;

        await using var reader = await sourceCommand.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess, cancellationToken);
        var planner = new ColumnMappingPlanner();
        var plan = await planner.CreatePlanAsync(destinationConnection, destinationObject, reader, mappings, options.Timeout, cancellationToken);
        plan.WriteSummary(_console);

        if (plan.Mappings.Count == 0)
        {
            throw new InvalidOperationException("No compatible columns were mapped. Nothing to copy.");
        }

        SqlTransaction? transaction = null;
        try
        {
            if (options.Transaction)
            {
                transaction = (SqlTransaction)await destinationConnection.BeginTransactionAsync(cancellationToken);
            }

            if (options.Mode is DestinationMode.Truncate or DestinationMode.Delete)
            {
                await ExecuteDestinationModeAsync(destinationConnection, transaction, destinationObject, options.Mode, options.Timeout, cancellationToken);
            }

            var rowsCopied = await WriteToServerAsync(destinationConnection, transaction, destinationObject, reader, plan, options, cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            stopwatch.Stop();
            var elapsedSeconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001d);
            var rate = rowsCopied / elapsedSeconds;
            _console.WriteLine();
            _console.WriteLine("Completed");
            _console.WriteLine($"Rows copied: {rowsCopied:N0}");
            _console.WriteLine($"Elapsed:     {stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}");
            _console.WriteLine($"Rate:        {rate:N0} rows/sec");
            return 0;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    private async Task ExecuteDestinationModeAsync(
        SqlConnection destinationConnection,
        SqlTransaction? transaction,
        SqlObjectName destinationObject,
        DestinationMode mode,
        int timeout,
        CancellationToken cancellationToken)
    {
        var commandText = mode switch
        {
            DestinationMode.Truncate => $"TRUNCATE TABLE {destinationObject}",
            DestinationMode.Delete => $"DELETE FROM {destinationObject}",
            _ => throw new InvalidOperationException($"Unsupported destination mode '{mode}'.")
        };

        await using var command = destinationConnection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = timeout;
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<long> WriteToServerAsync(
        SqlConnection destinationConnection,
        SqlTransaction? transaction,
        SqlObjectName destinationObject,
        SqlDataReader reader,
        ColumnMappingPlan plan,
        CopyCommandOptions options,
        CancellationToken cancellationToken)
    {
        var rowsCopied = 0L;
        using var bulkCopy = new SqlBulkCopy(destinationConnection, SqlBulkCopyOptionsBuilder.Build(options), transaction)
        {
            DestinationTableName = destinationObject.ToString(),
            BatchSize = options.BatchSize,
            BulkCopyTimeout = options.BulkTimeout,
            NotifyAfter = options.NotifyAfter
        };

        foreach (var mapping in plan.Mappings)
        {
            bulkCopy.ColumnMappings.Add(mapping.SourceColumn, mapping.DestinationColumn);
        }

        bulkCopy.SqlRowsCopied += (_, eventArgs) =>
        {
            rowsCopied = eventArgs.RowsCopied;
            _console.WriteLine($"{eventArgs.RowsCopied:N0} rows");
        };

        await bulkCopy.WriteToServerAsync(reader, cancellationToken);
        if (rowsCopied == 0)
        {
            rowsCopied = plan.Mappings.Count > 0 ? bulkCopy.RowsCopied : 0;
        }

        return rowsCopied;
    }
}
