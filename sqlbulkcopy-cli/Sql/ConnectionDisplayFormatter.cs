using Microsoft.Data.SqlClient;

namespace sqlbulkcopy_cli.Sql;

internal static class ConnectionDisplayFormatter
{
    public static string Format(SqlConnection connection)
    {
        var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
        var dataSource = string.IsNullOrWhiteSpace(builder.DataSource) ? "(unknown-server)" : builder.DataSource;
        var database = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "(default-database)" : builder.InitialCatalog;
        return $"{dataSource} / {database}";
    }
}
