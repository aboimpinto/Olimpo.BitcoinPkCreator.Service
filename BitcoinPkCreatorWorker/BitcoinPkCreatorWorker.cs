using BitcoinPkCreatorWorker.Extentions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDbQueueService;
using MongoDbQueueService.Configuration;
// using WorkerUtilitiesService;

namespace BitcoinPkCreatorWorker
{
    public class BitcoinPkCreatorWorker : BackgroundService
    {
        private readonly ILogger<BitcoinPkCreatorWorker> _logger;

        private SubscriberSettings _subscriberSettings;
        private PublisherSettings _publishSettings;
        private ISubscriber _subscriber;
        private IPublisher _publisher;
        private readonly IPublicKeyCreatorService _publicKeyCreatorService;
        // private readonly IWorkerLifeCycleService _workerLifeCycleService;

        public BitcoinPkCreatorWorker(
            IPublicKeyCreatorService publicKeyCreatorService,
            // IWorkerLifeCycleService workerLifeCycleService,
            ILogger<BitcoinPkCreatorWorker> logger)
        {
            this._publicKeyCreatorService = publicKeyCreatorService;
            // this._workerLifeCycleService = workerLifeCycleService;
            this._logger = logger;

            this.ReadConfigurations();

            this._logger.LogInformation($"Subscriber ConnectionString: {this._subscriberSettings.ConnectionString}");
            this._subscriber = new Subscriber(
                this._subscriberSettings.ConnectionString,
                this._subscriberSettings.Database,
                this._subscriberSettings.Queue,
                this._subscriberSettings.WorkerName);

            this._publisher = new Publisher(
                this._publishSettings.ConnectionString,
                this._publishSettings.Database,
                this._publishSettings.Queue);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("Worker started at: {0}", DateTimeOffset.Now);

            // await this._workerLifeCycleService
            //     .StartWorker()
            //     .ConfigureAwait(false);

            this._publicKeyCreatorService.OnNewPublicAddress
                .Subscribe(async x => 
                {
                    await this._publisher.SendAsync<PublicAddress>(x);
                });

            var queueSubscriber = this._subscriber
                .SubscribeQueueCollection<PrivateKeyAddress>(stoppingToken)
                .Subscribe(x => 
                {
                    this.CreatePublicKeys(x.Payload.PrivateKeyBytes);

                    x.ProcessSucessful = false;
                });
        }

        private void ReadConfigurations()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            this._subscriberSettings = new SubscriberSettings();
            configuration.Bind("SubscriberSettings", this._subscriberSettings);

            this._publishSettings = new PublisherSettings();
            configuration.Bind("PublisherSettings", this._publishSettings);
        }

        private void CreatePublicKeys(byte[] source)
        {
            this._logger.LogInformation("Start creating public keys for [{0}]", source.ToDescription());
            this._publicKeyCreatorService.CreatePublicKeys(source);
        }
    }
}