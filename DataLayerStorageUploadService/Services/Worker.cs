using DataLayerStorageUploadService;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;

public class Worker
{
    private IWebHost _webHost;

    public void Start()
    {
        var config = Utils.GetConfig();
        var clientAccessKey = config["CLIENT_ACCESS_KEY"].ToString();
        var clientSecretAccessKey = config["CLIENT_SECRET_ACCESS_KEY"].ToString();

        if (string.IsNullOrEmpty(clientAccessKey) || string.IsNullOrEmpty(clientSecretAccessKey))
        {
            Console.WriteLine("CLIENT_ACCESS_KEY or CLIENT_SECRET_ACCESS_KEY is missing. Please update the configuration.");
            Environment.Exit(0);
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

    public void Stop()
    {
        _webHost?.Dispose();
    }
}
