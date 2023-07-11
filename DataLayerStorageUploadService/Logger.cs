using Serilog;
using System;
using System.IO;

public static class Logger
{
    private static readonly string HomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string PersistenceFolderPath = DataLayerStorageUploadService.Utils.GetDLaaSRootEnv();
    private static readonly string LogDir = Path.Combine(PersistenceFolderPath, "logs");

    static Logger()
    {
        if (!Directory.Exists(LogDir))
        {
            Directory.CreateDirectory(LogDir);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(LogDir, "log-.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public static void LogInformation(string message)
    {
        Console.WriteLine(message); // Write to the console
        Log.Information(message);
    }

    public static void LogError(string message)
    {
        Console.WriteLine(message); // Write to the console
        Log.Error(message);
    }
}
