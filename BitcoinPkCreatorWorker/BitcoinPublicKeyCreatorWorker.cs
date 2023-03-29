using BitcoinPkCreatorWorker.Extentions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDbQueueService;

namespace BitcoinPkCreatorWorker
{
    public class BitcoinPublicKeyCreatorWorker : BackgroundService
    {
        private readonly ILogger<BitcoinPublicKeyCreatorWorker> _logger;

        private ISubscriber _subscriber;
        private IPublisher _publisher;
        private readonly IPublicKeyCreatorService _publicKeyCreatorService;

        public BitcoinPublicKeyCreatorWorker(
            IPublicKeyCreatorService publicKeyCreatorService,
            ILogger<BitcoinPublicKeyCreatorWorker> logger)
        {
            this._publicKeyCreatorService = publicKeyCreatorService;
            this._logger = logger;

            this._subscriber = new Subscriber(true);
            this._publisher = new Publisher(true);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("Worker started at: {0}", DateTimeOffset.Now);

            this._publicKeyCreatorService.OnNewPublicAddress
                .Subscribe(async x => 
                {
                    await this._publisher.SendAsync<PublicAddress>(x);
                });

            this._subscriber
                .SubscribeQueueCollection<PrivateKeyAddress>(stoppingToken)
                .Subscribe(x => 
                {
                    this.CreatePublicKeys(x.Payload.PrivateKeyBytes);
                    x.ProcessSucessful = true;
                });

            return Task.CompletedTask;
        }

        private void CreatePublicKeys(byte[] source)
        {
            this._logger.LogInformation("Start creating public keys for [{0}]", source.ToDescription());
            this._publicKeyCreatorService.CreatePublicKeys(source);
        }
    }
}