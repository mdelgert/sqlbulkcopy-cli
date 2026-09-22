using sqlbulkcopy_cli.Console;

namespace sqlbulkcopy_cli.Copy;

internal sealed class ColumnMappingPlan
{
    public ColumnMappingPlan(
        IReadOnlyList<ColumnMapping> mappings,
        IReadOnlyList<string> unmatchedSourceColumns,
        IReadOnlyList<string> unmatchedDestinationColumns,
        IReadOnlyList<string> incompatibleColumns)
    {
        Mappings = mappings;
        UnmatchedSourceColumns = unmatchedSourceColumns;
        UnmatchedDestinationColumns = unmatchedDestinationColumns;
        IncompatibleColumns = incompatibleColumns;
    }

    public IReadOnlyList<ColumnMapping> Mappings { get; }
    public IReadOnlyList<string> UnmatchedSourceColumns { get; }
    public IReadOnlyList<string> UnmatchedDestinationColumns { get; }
    public IReadOnlyList<string> IncompatibleColumns { get; }

    public void WriteSummary(IConsoleWriter console)
    {
        console.WriteLine($"Mapped columns: {Mappings.Count:N0}");

        if (UnmatchedSourceColumns.Count > 0)
        {
            console.WriteLine($"Unmatched source columns: {string.Join(", ", UnmatchedSourceColumns)}");
        }

        if (UnmatchedDestinationColumns.Count > 0)
        {
            console.WriteLine($"Unmatched destination columns: {string.Join(", ", UnmatchedDestinationColumns)}");
        }

        if (IncompatibleColumns.Count > 0)
        {
            console.WriteLine($"Skipped incompatible columns: {string.Join(", ", IncompatibleColumns)}");
        }

        console.WriteLine();
    }
}
