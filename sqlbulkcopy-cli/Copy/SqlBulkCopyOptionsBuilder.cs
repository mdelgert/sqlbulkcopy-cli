using Microsoft.Data.SqlClient;

namespace sqlbulkcopy_cli.Copy;

internal static class SqlBulkCopyOptionsBuilder
{
    public static SqlBulkCopyOptions Build(CopyCommandOptions options)
    {
        var result = SqlBulkCopyOptions.Default;

        if (options.KeepIdentity)
        {
            result |= SqlBulkCopyOptions.KeepIdentity;
        }

        if (options.KeepNulls)
        {
            result |= SqlBulkCopyOptions.KeepNulls;
        }

        if (options.CheckConstraints)
        {
            result |= SqlBulkCopyOptions.CheckConstraints;
        }

        if (options.TableLock)
        {
            result |= SqlBulkCopyOptions.TableLock;
        }

        if (options.FireTriggers)
        {
            result |= SqlBulkCopyOptions.FireTriggers;
        }

        if (options.UseInternalTransaction)
        {
            result |= SqlBulkCopyOptions.UseInternalTransaction;
        }

        return result;
    }
}
