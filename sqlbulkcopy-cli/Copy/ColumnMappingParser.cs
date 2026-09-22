namespace sqlbulkcopy_cli.Copy;

internal static class ColumnMappingParser
{
    public static IReadOnlyList<ColumnMapping> Parse(IReadOnlyList<string> mappings)
    {
        if (mappings.Count == 0)
        {
            return [];
        }

        var result = new List<ColumnMapping>(mappings.Count);
        foreach (var mapping in mappings)
        {
            var parts = mapping.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                throw new InvalidOperationException($"Invalid mapping '{mapping}'. Expected source=destination.");
            }

            result.Add(new ColumnMapping(parts[0], parts[1]));
        }

        return result;
    }
}
