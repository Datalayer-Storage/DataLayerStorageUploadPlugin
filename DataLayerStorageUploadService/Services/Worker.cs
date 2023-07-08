using DataLayerStorageUploadService;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using System;
using System.IO;

public class Worker
{
    private IWebHost _webHost;

    public void Start()
    {
        try
        {
            var config = Utils.GetConfig();
            var clientAccessKey = config["CLIENT_ACCESS_KEY"].ToString();
            var clientSecretAccessKey = config["CLIENT_SECRET_ACCESS_KEY"].ToString();

            if (string.IsNullOrEmpty(clientAccessKey) || string.IsNullOrEmpty(clientSecretAccessKey))
            {
                Logger.LogError("CLIENT_ACCESS_KEY or CLIENT_SECRET_ACCESS_KEY is missing. Please update the configuration.");
                Logger.LogInformation(config.ToString());
            }

            Utils.InitiateAddMissingFilesOnLocal();

            var port = config["PORT"] ?? "41410";
            var url = $"http://localhost:{port}";

            Logger.LogInformation("Running on " + url);

            _webHost = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseUrls(url)
                .Build();

            _webHost.Start();
        }
        catch (Exception ex)
        {
            Logger.LogError($"An error occurred: {ex.Message}");
            throw; // rethrow the exception so Topshelf can handle it
        }
        finally
        {
            // Ensures that all logs are written out
            Log.CloseAndFlush();
        }
    }

    public void Stop()
    {
        _webHost?.Dispose();
    }
}
