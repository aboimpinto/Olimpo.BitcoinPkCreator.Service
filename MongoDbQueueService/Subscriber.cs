using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;

namespace MongoDbQueueService
{
    public class Subscriber : ISubscriber
    {
        private readonly string _workerName;
        private readonly bool _deleteOnAcknowledge;
        private IMongoDatabase _database;
        private IMongoCollection<QueueCollection> _queueCollection;

        public Subscriber(string url, string database, string collection, string workerName, bool deleteOnAcknowledge = false)
        {
            var client = new MongoClient(url);
            this._database = client.GetDatabase(database);
            this._queueCollection = this._database.GetCollection<QueueCollection>(collection);
            this._workerName = workerName;
            this._deleteOnAcknowledge = deleteOnAcknowledge;
        }

        public IObservable<SubscriptionResult<T>> SubscribeQueueCollection<T>(CancellationToken token)
        {
            var scheduleInstance = ThreadPoolScheduler.Instance;

            return Observable.Create<SubscriptionResult<T>>(item =>
            {
                var disposable = Observable
                    .Interval(TimeSpan.FromSeconds(1), scheduleInstance)
                    .Subscribe(async _ => 
                    {
                        var sortOptions = Builders<QueueCollection>.Sort.Ascending("LastTimeChanged");
                        var sort = new FindOptions<QueueCollection>
                        {
                            Sort = sortOptions
                        };

                        var itemProcessingForWorker = await this._queueCollection
                            .FindAsync(x => x.WorkerName == this._workerName, sort)
                            .Result
                            .SingleOrDefaultAsync();

                        if (itemProcessingForWorker != null)
                        {
                            return;
                        }

                        var filter = Builders<QueueCollection>.Filter.And
                        (
                            Builders<QueueCollection>.Filter.Eq(x => x.WorkerName, string.Empty),
                            Builders<QueueCollection>.Filter.Eq(x => x.Processed, false)
                        );
                        var update = Builders<QueueCollection>.Update.Set(x => x.WorkerName, this._workerName);
                        var result = await this._queueCollection.UpdateOneAsync(filter, update);

                        var itemFromQueue = await this._queueCollection
                            .FindAsync(x => x.WorkerName == this._workerName)
                            .Result
                            .SingleOrDefaultAsync();

                        if (itemFromQueue != null)
                        {
                            try
                            {
                                var jsonFromDocument = itemFromQueue.Payload.ToJson();
                                var deserializedObject = JsonSerializer.Deserialize<T>(jsonFromDocument);

                                var subscriptionResult = new SubscriptionResult<T>();
                                subscriptionResult.ProcessSucessful = false;
                                subscriptionResult.Payload = deserializedObject;

                                item.OnNext(subscriptionResult);

                                if (subscriptionResult.ProcessSucessful)
                                {
                                    if (this._deleteOnAcknowledge)
                                    {
                                        await this.AcknowledgeAndDelete()
                                            .ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await this.AcknowledgeWithoutDelete(
                                                JsonSerializer.Serialize<T>(subscriptionResult.Payload), 
                                                subscriptionResult.ProcessSucessful)
                                            .ConfigureAwait(false);    
                                    }
                                }
                                else 
                                {
                                    await this.AcknowledgeWithoutDelete(
                                            JsonSerializer.Serialize<T>(subscriptionResult.Payload), 
                                            subscriptionResult.ProcessSucessful)
                                        .ConfigureAwait(false);
                                }
                            }
                            catch
                            {
                                throw new InvalidOperationException($"Was not possible to process payload: {itemFromQueue.Payload}");
                            }
                        }

                    });
                token.Register(() => disposable.Dispose());

                return Disposable.Empty;
            });
        }

        private async Task AcknowledgeAndDelete()
        {
            var filter = Builders<QueueCollection>.Filter.Eq(x => x.WorkerName, this._workerName);
            var item = await this._queueCollection
                .FindAsync(filter)
                .Result
                .SingleAsync()
                .ConfigureAwait(false);

            if (item == null)
            {
                throw new InvalidOperationException($"Cannot Acknowledge last operation from Worker: {this._workerName}");
            }

            await this._queueCollection
                .DeleteOneAsync(filter)
                .ConfigureAwait(false);
        }

        private async Task AcknowledgeWithoutDelete(string payload, bool processedSuccessful)
        {
            var filter = Builders<QueueCollection>.Filter.Eq(x => x.WorkerName, this._workerName);
            var item = await this._queueCollection
                .FindAsync(filter)
                .Result
                .SingleAsync()
                .ConfigureAwait(false);

            if (item == null)
            {
                throw new InvalidOperationException($"Cannot Acknowledge last operation from Worker: {this._workerName}");
            }

            item.WorkerName = string.Empty;
            item.Processed = true;

            var update = Builders<QueueCollection>.Update
                .Set(x => x.WorkerName, string.Empty)
                .Set(x => x.Processed, processedSuccessful)
                .Set(x => x.LastTimeChanged, DateTime.UtcNow)
                .Set(x => x.Payload, BsonDocument.Parse(payload));

            await this._queueCollection
                .UpdateOneAsync(filter, update)
                .ConfigureAwait(false);
        }
    }
}
