namespace sqlbulkcopy_cli.Configuration;

internal sealed class AppSettings
{
    public string? SourceConnection { get; set; }
    public string? SourceQuery { get; set; }
    public string? SourceObject { get; set; }
    public string? DestinationConnection { get; set; }
    public string? DestinationTable { get; set; }
    public string? Mapping { get; set; }
    public List<string> Map { get; set; } = [];
    public string? Mode { get; set; }
    public bool? ConfirmDestructive { get; set; }
    public int? BatchSize { get; set; }
    public int? Timeout { get; set; }
    public int? BulkTimeout { get; set; }
    public int? NotifyAfter { get; set; }
    public bool? Transaction { get; set; }
    public bool? KeepIdentity { get; set; }
    public bool? KeepNulls { get; set; }
    public bool? CheckConstraints { get; set; }
    public bool? TableLock { get; set; }
    public bool? FireTriggers { get; set; }
    public bool? UseInternalTransaction { get; set; }
}
