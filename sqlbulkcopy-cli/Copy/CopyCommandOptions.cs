namespace sqlbulkcopy_cli.Copy;

internal sealed record CopyCommandOptions(
    string? SourceConnection,
    string? SourceQuery,
    string? SourceObject,
    string? DestinationConnection,
    string? DestinationTable,
    string Mapping,
    IReadOnlyList<string> Maps,
    DestinationMode Mode,
    bool ConfirmDestructive,
    int BatchSize,
    int Timeout,
    int BulkTimeout,
    int NotifyAfter,
    bool Transaction,
    bool KeepIdentity,
    bool KeepNulls,
    bool CheckConstraints,
    bool TableLock,
    bool FireTriggers,
    bool UseInternalTransaction);
