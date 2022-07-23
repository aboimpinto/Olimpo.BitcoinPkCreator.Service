using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerUtilitiesService;

namespace BitcoinPkCreatorWorker
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureLogging(x => 
                {
                    x.ClearProviders();
                    x.AddConsole();
                    x.AddDebug();
                })
                .ConfigureServices((hostContext, services) => {
                    
                    // TODO [AboimPinto]: this should load the configurations and expose to the Worker
                    // CollectConfiguration(services);         

                    services.AddSingleton<IPublicKeyCreatorService, PublicKeyCreatorService>();
                    services.AddSingleton<IWorkerLifeCycleService, WorkerLifeCycleService>();

                    services.AddHostedService<BitcoinPkCreatorWorker>();

                });

        // private static void CollectConfiguration(IServiceCollection services)
        // {

        // }
    }
}