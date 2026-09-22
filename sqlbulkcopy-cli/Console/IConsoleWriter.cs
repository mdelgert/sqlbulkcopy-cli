namespace sqlbulkcopy_cli.Console;

internal interface IConsoleWriter
{
    void WriteLine(string value = "");
    void WriteErrorLine(string value);
    bool IsInteractive { get; }
}
