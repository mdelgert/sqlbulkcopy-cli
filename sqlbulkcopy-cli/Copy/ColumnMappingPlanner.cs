using System.Data.Common;
using Microsoft.Data.SqlClient;
using sqlbulkcopy_cli.Sql;

namespace sqlbulkcopy_cli.Copy;

internal sealed class ColumnMappingPlanner
{
    public async Task<ColumnMappingPlan> CreatePlanAsync(
        SqlConnection destinationConnection,
        SqlObjectName destinationObject,
        SqlDataReader reader,
        IReadOnlyList<ColumnMapping> explicitMappings,
        int timeout,
        CancellationToken cancellationToken)
    {
        var sourceColumns = reader.GetColumnSchema()
            .Select(column => new ColumnMetadata(column.ColumnName ?? string.Empty, NormalizeTypeName(column.DataTypeName)))
            .Where(column => !string.IsNullOrWhiteSpace(column.Name))
            .ToList();

        var destinationColumns = await GetDestinationColumnsAsync(destinationConnection, destinationObject, timeout, cancellationToken);
        return explicitMappings.Count > 0
            ? BuildExplicitPlan(sourceColumns, destinationColumns, explicitMappings)
            : BuildAutomaticPlan(sourceColumns, destinationColumns);
    }

    private static ColumnMappingPlan BuildAutomaticPlan(IReadOnlyList<ColumnMetadata> sourceColumns, IReadOnlyList<ColumnMetadata> destinationColumns)
    {
        var sourceByName = sourceColumns.ToDictionary(column => column.Name, StringComparer.OrdinalIgnoreCase);
        var destinationByName = destinationColumns.ToDictionary(column => column.Name, StringComparer.OrdinalIgnoreCase);
        var mappings = new List<ColumnMapping>();
        var incompatible = new List<string>();

        foreach (var sourceColumn in sourceColumns)
        {
            if (!destinationByName.TryGetValue(sourceColumn.Name, out var destinationColumn))
            {
                continue;
            }

            if (!AreCompatible(sourceColumn.TypeName, destinationColumn.TypeName))
            {
                incompatible.Add($"{sourceColumn.Name} ({sourceColumn.TypeName ?? "unknown"} -> {destinationColumn.TypeName ?? "unknown"})");
                continue;
            }

            mappings.Add(new ColumnMapping(sourceColumn.Name, destinationColumn.Name));
        }

        var unmatchedSource = sourceColumns
            .Where(column => !destinationByName.ContainsKey(column.Name))
            .Select(column => column.Name)
            .ToList();
        var unmatchedDestination = destinationColumns
            .Where(column => !sourceByName.ContainsKey(column.Name))
            .Select(column => column.Name)
            .ToList();

        return new ColumnMappingPlan(mappings, unmatchedSource, unmatchedDestination, incompatible);
    }

    private static ColumnMappingPlan BuildExplicitPlan(
        IReadOnlyList<ColumnMetadata> sourceColumns,
        IReadOnlyList<ColumnMetadata> destinationColumns,
        IReadOnlyList<ColumnMapping> explicitMappings)
    {
        var sourceByName = sourceColumns.ToDictionary(column => column.Name, StringComparer.OrdinalIgnoreCase);
        var destinationByName = destinationColumns.ToDictionary(column => column.Name, StringComparer.OrdinalIgnoreCase);
        var mappings = new List<ColumnMapping>(explicitMappings.Count);

        foreach (var mapping in explicitMappings)
        {
            if (!sourceByName.TryGetValue(mapping.SourceColumn, out var sourceColumn))
            {
                throw new InvalidOperationException($"Source column '{mapping.SourceColumn}' was not found in the result set.");
            }

            if (!destinationByName.TryGetValue(mapping.DestinationColumn, out var destinationColumn))
            {
                throw new InvalidOperationException($"Destination column '{mapping.DestinationColumn}' was not found in the destination table.");
            }

            if (!AreCompatible(sourceColumn.TypeName, destinationColumn.TypeName))
            {
                throw new InvalidOperationException($"Source column '{mapping.SourceColumn}' is not compatible with destination column '{mapping.DestinationColumn}'.");
            }

            mappings.Add(new ColumnMapping(sourceColumn.Name, destinationColumn.Name));
        }

        return new ColumnMappingPlan(mappings, [], [], []);
    }

    private static bool AreCompatible(string? sourceTypeName, string? destinationTypeName)
    {
        var sourceFamily = GetTypeFamily(sourceTypeName);
        var destinationFamily = GetTypeFamily(destinationTypeName);
        return sourceFamily is not ColumnTypeFamily.Unknown && sourceFamily == destinationFamily;
    }

    private static ColumnTypeFamily GetTypeFamily(string? typeName)
    {
        return NormalizeTypeName(typeName) switch
        {
            null => ColumnTypeFamily.Unknown,
            "bigint" or "int" or "smallint" or "tinyint" or "bit" => ColumnTypeFamily.Integer,
            "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => ColumnTypeFamily.Numeric,
            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" or "time" => ColumnTypeFamily.Temporal,
            "char" or "nchar" or "varchar" or "nvarchar" or "text" or "ntext" or "xml" => ColumnTypeFamily.Text,
            "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => ColumnTypeFamily.Binary,
            "uniqueidentifier" => ColumnTypeFamily.Guid,
            _ => ColumnTypeFamily.Other
        };
    }

    private static string? NormalizeTypeName(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        var normalized = typeName.Trim().ToLowerInvariant();
        var parenthesisIndex = normalized.IndexOf('(');
        return parenthesisIndex >= 0 ? normalized[..parenthesisIndex] : normalized;
    }

    private static async Task<IReadOnlyList<ColumnMetadata>> GetDestinationColumnsAsync(
        SqlConnection connection,
        SqlObjectName destinationObject,
        int timeout,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT c.name, ty.name AS type_name
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
INNER JOIN sys.columns AS c ON c.object_id = t.object_id
INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
WHERE s.name = @schemaName AND t.name = @tableName
ORDER BY c.column_id;
""";

        await using var command = connection.CreateCommand();
        command.CommandTimeout = timeout;
        command.CommandText = sql;
        command.Parameters.AddWithValue("@schemaName", destinationObject.Schema);
        command.Parameters.AddWithValue("@tableName", destinationObject.Name);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = new List<ColumnMetadata>();
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnMetadata(reader.GetString(0), NormalizeTypeName(reader.GetString(1))));
        }

        if (columns.Count == 0)
        {
            throw new InvalidOperationException($"Destination table {destinationObject} was not found.");
        }

        return columns;
    }

    private enum ColumnTypeFamily
    {
        Unknown,
        Integer,
        Numeric,
        Temporal,
        Text,
        Binary,
        Guid,
        Other
    }
}
