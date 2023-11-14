using Topshelf;
namespace CacheServerConcole
{
    internal static class ConfigureService
    {
        /// <summary>
        /// To run the service using topself hostfactory
        /// </summary>
        internal static void Configure()
        {
            HostFactory.Run(configure =>
            {
                configure.Service<CacheService>(service =>
                {
                    service.ConstructUsing(s => new CacheService());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });
                //Setup Account that window service use to run.  
                configure.RunAsLocalSystem();
                configure.SetServiceName("CacheService");
                configure.SetDisplayName("CacheService");
                configure.SetDescription("CacheService: windows service with Topshelf");
            });
        }
    }
}