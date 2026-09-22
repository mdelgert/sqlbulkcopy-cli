using System.CommandLine;
using System.CommandLine.Parsing;
using sqlbulkcopy_cli.Configuration;
using sqlbulkcopy_cli.Console;

namespace sqlbulkcopy_cli.Copy;

internal static class CopyCommandFactory
{
    public static Command Create(AppSettings defaults, IConsoleWriter console)
    {
        var configOption = new Option<FileInfo?>("--config")
        {
            Description = "Optional JSON configuration file path."
        };
        var sourceConnectionOption = new Option<string?>("--source-connection")
        {
            Description = "Source SQL Server or Azure SQL connection string.",
            DefaultValueFactory = _ => defaults.SourceConnection
        };
        var sourceQueryOption = new Option<string?>("--source-query")
        {
            Description = "Source SQL query to execute.",
            DefaultValueFactory = _ => defaults.SourceQuery
        };
        var sourceObjectOption = new Option<string?>("--source-object")
        {
            Description = "Two-part source object name in schema.object format.",
            DefaultValueFactory = _ => defaults.SourceObject
        };
        var destinationConnectionOption = new Option<string?>("--destination-connection")
        {
            Description = "Destination SQL Server or Azure SQL connection string.",
            DefaultValueFactory = _ => defaults.DestinationConnection
        };
        var destinationTableOption = new Option<string?>("--destination-table")
        {
            Description = "Two-part destination table name in schema.table format.",
            DefaultValueFactory = _ => defaults.DestinationTable
        };
        var mappingOption = new Option<string>("--mapping")
        {
            Description = "Column mapping mode. Supported values: auto.",
            DefaultValueFactory = _ => defaults.Mapping ?? "auto"
        };
        var mapOption = new Option<string[]>("--map")
        {
            Description = "Explicit source=destination column mapping. Repeat for multiple mappings.",
            AllowMultipleArgumentsPerToken = false,
            DefaultValueFactory = _ => defaults.Map.ToArray()
        };
        var modeOption = new Option<DestinationMode>("--mode")
        {
            Description = "Destination mode: append, truncate, or delete.",
            DefaultValueFactory = _ => ParseMode(defaults.Mode)
        };
        var confirmDestructiveOption = new Option<bool>("--confirm-destructive")
        {
            Description = "Required for truncate or delete modes.",
            DefaultValueFactory = _ => defaults.ConfirmDestructive ?? false
        };
        var batchSizeOption = new Option<int>("--batch-size")
        {
            Description = "Rows per batch for SqlBulkCopy.",
            DefaultValueFactory = _ => defaults.BatchSize ?? 10_000
        };
        var timeoutOption = new Option<int>("--timeout")
        {
            Description = "Source and metadata command timeout in seconds.",
            DefaultValueFactory = _ => defaults.Timeout ?? 30
        };
        var bulkTimeoutOption = new Option<int>("--bulk-timeout")
        {
            Description = "SqlBulkCopy timeout in seconds. 0 means no limit.",
            DefaultValueFactory = _ => defaults.BulkTimeout ?? 0
        };
        var notifyAfterOption = new Option<int>("--notify-after")
        {
            Description = "Progress notification interval in rows.",
            DefaultValueFactory = _ => defaults.NotifyAfter ?? 10_000
        };
        var transactionOption = new Option<bool>("--transaction")
        {
            Description = "Wrap destination operations in a single transaction.",
            DefaultValueFactory = _ => defaults.Transaction ?? false
        };
        var keepIdentityOption = new Option<bool>("--keep-identity")
        {
            Description = "Preserve identity values.",
            DefaultValueFactory = _ => defaults.KeepIdentity ?? false
        };
        var keepNullsOption = new Option<bool>("--keep-nulls")
        {
            Description = "Preserve nulls instead of destination defaults.",
            DefaultValueFactory = _ => defaults.KeepNulls ?? false
        };
        var checkConstraintsOption = new Option<bool>("--check-constraints")
        {
            Description = "Check destination constraints during bulk copy.",
            DefaultValueFactory = _ => defaults.CheckConstraints ?? false
        };
        var tableLockOption = new Option<bool>("--table-lock")
        {
            Description = "Acquire a bulk update table lock.",
            DefaultValueFactory = _ => defaults.TableLock ?? false
        };
        var fireTriggersOption = new Option<bool>("--fire-triggers")
        {
            Description = "Fire insert triggers during bulk copy.",
            DefaultValueFactory = _ => defaults.FireTriggers ?? false
        };
        var useInternalTransactionOption = new Option<bool>("--use-internal-transaction")
        {
            Description = "Use SqlBulkCopy internal transaction handling.",
            DefaultValueFactory = _ => defaults.UseInternalTransaction ?? false
        };

        var command = new Command("copy", "Copy rows from a query or source object into a destination table.")
        {
            configOption,
            sourceConnectionOption,
            sourceQueryOption,
            sourceObjectOption,
            destinationConnectionOption,
            destinationTableOption,
            mappingOption,
            mapOption,
            modeOption,
            confirmDestructiveOption,
            batchSizeOption,
            timeoutOption,
            bulkTimeoutOption,
            notifyAfterOption,
            transactionOption,
            keepIdentityOption,
            keepNullsOption,
            checkConstraintsOption,
            tableLockOption,
            fireTriggersOption,
            useInternalTransactionOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            try
            {
                var options = new CopyCommandOptions(
                    parseResult.GetValue(sourceConnectionOption),
                    parseResult.GetValue(sourceQueryOption),
                    parseResult.GetValue(sourceObjectOption),
                    parseResult.GetValue(destinationConnectionOption),
                    parseResult.GetValue(destinationTableOption),
                    parseResult.GetValue(mappingOption) ?? "auto",
                    parseResult.GetValue(mapOption) ?? [],
                    parseResult.GetValue(modeOption),
                    parseResult.GetValue(confirmDestructiveOption),
                    parseResult.GetValue(batchSizeOption),
                    parseResult.GetValue(timeoutOption),
                    parseResult.GetValue(bulkTimeoutOption),
                    parseResult.GetValue(notifyAfterOption),
                    parseResult.GetValue(transactionOption),
                    parseResult.GetValue(keepIdentityOption),
                    parseResult.GetValue(keepNullsOption),
                    parseResult.GetValue(checkConstraintsOption),
                    parseResult.GetValue(tableLockOption),
                    parseResult.GetValue(fireTriggersOption),
                    parseResult.GetValue(useInternalTransactionOption));

                var runner = new CopyCommandRunner(console);
                return await runner.RunAsync(options, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                console.WriteErrorLine("Operation canceled.");
                return 130;
            }
            catch (Exception exception)
            {
                console.WriteErrorLine(exception.Message);
                return 1;
            }
        });

        return command;
    }

    private static DestinationMode ParseMode(string? mode) =>
        Enum.TryParse<DestinationMode>(mode, ignoreCase: true, out var parsed) ? parsed : DestinationMode.Append;
}
