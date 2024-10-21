using Reloaded.Mod.Interfaces;
using System.Drawing;

namespace p5rpc.CustomSaveDataFramework.Utils;

public static class Log
{
    private static string _modName = "Mod";
    private static ILogger? _logger;
    private static Color _informationColor = Color.White;
    private static LogLevel _logLevel;
    private static bool _alwaysAsync;

    public static void Initialize(string modName, ILogger logger, Color informationColor, LogLevel logLevel, bool alwaysAsync = false)
    {
        _modName = modName;
        _logger = logger;
        _informationColor = informationColor;
        _logLevel = logLevel;
        _alwaysAsync = alwaysAsync;
    }

    public static void Debug(string message, bool useAsync = false)
    {
        if (_logLevel < LogLevel.Information)
        {
            LogMessage(LogLevel.Debug, message, useAsync);
        }
    }

    public static void Information(string message, bool useAsync = false)
    {
        if (_logLevel < LogLevel.Warning)
        {
            LogMessage(LogLevel.Information, message, useAsync);
        }
    }

    public static void Warning(string message, bool useAsync = false)
    {
        if (_logLevel < LogLevel.Error)
        {
            LogMessage(LogLevel.Warning, message, useAsync);
        }
    }
    
    public static void Error(Exception ex, string message, bool useAsync = false)
    {
        LogMessage(LogLevel.Error, $"{message}\n{ex.Message}\n{ex.StackTrace}", useAsync);
    }

    public static void Error(string message, bool useAsync = false)
    {
        LogMessage(LogLevel.Error, message, useAsync);
    }

    private static void LogMessage(LogLevel level, string message, bool useAsync = false)
    {
        var color =
            level == LogLevel.Debug ? Color.LightGreen :
            level == LogLevel.Information ? _informationColor :
            level == LogLevel.Error ? Color.Red :
            level == LogLevel.Warning ? Color.LightGoldenrodYellow :
            Color.White;

        if (useAsync || _alwaysAsync)
        {
            _logger?.WriteLineAsync(FormatMessage(level, message), color);
        }
        else
        {
            _logger?.WriteLine(FormatMessage(level, message), color);
        }
    }

    private static string FormatMessage(LogLevel level, string message)
    {
        if (level == LogLevel.Information)
        {
            return $"[{_modName}] {message}";
        }
        
        var levelStr =
            level == LogLevel.Debug ? "[DBG]" :
            level == LogLevel.Warning ? "[WRN]" :
            level == LogLevel.Error ? "[ERR]" : string.Empty;

        return $"[{_modName}] {levelStr} {message}";
    }
}

public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error,
}