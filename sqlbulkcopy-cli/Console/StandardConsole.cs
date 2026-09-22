namespace sqlbulkcopy_cli.Console;

internal sealed class StandardConsole : IConsoleWriter
{
    public void WriteLine(string value = "") => System.Console.Out.WriteLine(value);

    public void WriteErrorLine(string value) => System.Console.Error.WriteLine(value);

    public bool IsInteractive => !System.Console.IsOutputRedirected;
}
