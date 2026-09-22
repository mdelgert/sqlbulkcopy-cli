namespace sqlbulkcopy_cli.Copy;

internal static class CopyCommandValidator
{
    public static void Validate(CopyCommandOptions options, bool isInteractive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SourceConnection);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DestinationConnection);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DestinationTable);

        var hasSourceQuery = !string.IsNullOrWhiteSpace(options.SourceQuery);
        var hasSourceObject = !string.IsNullOrWhiteSpace(options.SourceObject);
        if (hasSourceQuery == hasSourceObject)
        {
            throw new InvalidOperationException("Specify either --source-query or --source-object, but not both.");
        }

        if (!string.Equals(options.Mapping, "auto", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only --mapping auto is currently supported.");
        }

        if (options.BatchSize <= 0)
        {
            throw new InvalidOperationException("--batch-size must be greater than 0.");
        }

        if (options.Timeout < 0)
        {
            throw new InvalidOperationException("--timeout must be 0 or greater.");
        }

        if (options.BulkTimeout < 0)
        {
            throw new InvalidOperationException("--bulk-timeout must be 0 or greater.");
        }

        if (options.NotifyAfter <= 0)
        {
            throw new InvalidOperationException("--notify-after must be greater than 0.");
        }

        if (options.Transaction && options.UseInternalTransaction)
        {
            throw new InvalidOperationException("--transaction and --use-internal-transaction cannot be used together.");
        }

        if (options.Mode is DestinationMode.Truncate or DestinationMode.Delete && !options.ConfirmDestructive)
        {
            var guidance = isInteractive
                ? "Pass --confirm-destructive to continue with the requested destructive mode."
                : "Automation must explicitly pass --confirm-destructive for truncate or delete mode.";
            throw new InvalidOperationException(guidance);
        }
    }
}
