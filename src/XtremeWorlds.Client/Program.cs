using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using XtremeWorlds.Client.Features;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
    .CreateLogger();

General.Client.Run();