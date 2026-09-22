namespace sqlbulkcopy_cli.Configuration;

internal static class BootstrapArguments
{
    public static string? GetConfigPath(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count; index++)
        {
            var current = args[index];
            if (string.Equals(current, "--config", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Count)
                {
                    throw new InvalidOperationException("The --config option requires a value.");
                }

                return args[index + 1];
            }

            const string prefix = "--config=";
            if (current.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return current[prefix.Length..];
            }
        }

        return null;
    }
}
