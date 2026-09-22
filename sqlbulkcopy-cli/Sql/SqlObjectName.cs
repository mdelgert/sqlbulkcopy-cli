using System.Text.RegularExpressions;

namespace sqlbulkcopy_cli.Sql;

internal sealed partial record SqlObjectName(string Schema, string Name)
{
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_@$#]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();

    public static SqlObjectName ParseRequired(string? value, string displayName)
    {
        if (!TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"The {displayName} must be provided as schema.object using simple SQL identifiers.");
        }

        return parsed;
    }

    public static bool TryParse(string? value, out SqlObjectName parsed)
    {
        parsed = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!IdentifierRegex().IsMatch(parts[0]) || !IdentifierRegex().IsMatch(parts[1]))
        {
            return false;
        }

        parsed = new SqlObjectName(parts[0], parts[1]);
        return true;
    }

    public static string QuoteIdentifier(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";

    public override string ToString() => $"{QuoteIdentifier(Schema)}.{QuoteIdentifier(Name)}";
}
