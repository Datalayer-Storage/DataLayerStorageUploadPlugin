using System;
using Topshelf;

public class Program
{
    static void Main()
    {
        var exitCode = HostFactory.Run(x =>
        {
            x.Service<Worker>(s =>
            {
                s.ConstructUsing(name => new Worker());
                s.WhenStarted(tc => tc.Start());
                s.WhenStopped(tc => tc.Stop());
            });
            x.RunAsLocalSystem();

            x.OnException(e =>
            {
                Logger.LogInformation("An error occurred " + e.Message);
            });

            x.RunAsLocalSystem();
            x.SetDescription("Chia Upload Plugin");
            x.SetDisplayName("DataLayer Storage");
            x.SetServiceName("DataLayerStorage");
        });

        int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
        Environment.ExitCode = exitCodeValue;
    }
}
