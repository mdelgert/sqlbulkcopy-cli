namespace sqlbulkcopy_cli.Sql;

internal static class SqlQueryBuilder
{
    public static string BuildSourceQuery(string? sourceQuery, SqlObjectName? sourceObject)
    {
        if (!string.IsNullOrWhiteSpace(sourceQuery))
        {
            return sourceQuery;
        }

        if (sourceObject is null)
        {
            throw new InvalidOperationException("A source query or source object is required.");
        }

        return $"SELECT * FROM {sourceObject}";
    }
}
