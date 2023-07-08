// Program.cs
namespace DataLayerStorageUploadService
{
    using Topshelf;

    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<Worker>(s =>
                {
                    s.ConstructUsing(name => new Worker());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Chia Upload Plugin");
                x.SetDisplayName("DataLayer Storage");
                x.SetServiceName("DataLayerStorage");
            });
        }
    }
}
