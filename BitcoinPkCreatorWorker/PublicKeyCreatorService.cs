using System.Reactive.Subjects;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using NBitcoin;

namespace BitcoinPkCreatorWorker
{
    public class PublicKeyCreatorService : IPublicKeyCreatorService
    {
        private PkDatabaseSettings _pkDatabaseSettings;
        private IMongoDatabase _database;
        private IMongoCollection<PublicAddress> _publicAddressesCollection;

        public Subject<PublicAddress> OnNewPublicAddress { get; }

        public PublicKeyCreatorService()
        {
            this.OnNewPublicAddress = new Subject<PublicAddress>();

            this.ReadConfigurations();

            var client =  new MongoClient();
            this._database = client.GetDatabase(this._pkDatabaseSettings.Database);
            this._publicAddressesCollection = this._database.GetCollection<PublicAddress>("PublicAddress");
        }

        public void CreatePublicKeys(byte[] source)
        {
            var key = new Key(source);
            var wif = key.GetWif(Network.Main);

            var secret = new BitcoinSecret(key, Network.Main);
            var privateKey = secret.ToWif();
            var legacyPublicAddress = secret.GetAddress(ScriptPubKeyType.Legacy);
            var segwitPublicAddress = secret.GetAddress(ScriptPubKeyType.Segwit);
            var segwitP2SHPublicAddress = secret.GetAddress(ScriptPubKeyType.SegwitP2SH);

            var legacyAddress = new PublicAddress
            {
                Address = secret.GetAddress(ScriptPubKeyType.Legacy).ToString(),
                Wif = secret.ToWif(),
                Type = "Legacy",
                LastVerification = null,
                Balance = 0,
                TransactionCount = 0
            };
            this.OnNewPublicAddress.OnNext(legacyAddress);

            var segwitAddress = new PublicAddress
            {
                Address = secret.GetAddress(ScriptPubKeyType.Segwit).ToString(),
                Wif = secret.ToWif(),
                Type = "Segwit",
                LastVerification = null,
                Balance = 0,
                TransactionCount = 0
            };
            this.OnNewPublicAddress.OnNext(segwitAddress);

            var segwitP2SHAddress = new PublicAddress
            {
                Address = secret.GetAddress(ScriptPubKeyType.SegwitP2SH).ToString(),
                Wif = secret.ToWif(),
                Type = "SegwitP2SH",
                LastVerification = null,
                Balance = 0,
                TransactionCount = 0
            };
            this.OnNewPublicAddress.OnNext(segwitP2SHAddress);

            // this._publicAddressesCollection.InsertMany(new[] { legacyAddress, segwitAddress, segwitP2SHAddress });

            Console.WriteLine("Legacy Address: {0}", legacyPublicAddress);
            Console.WriteLine("Swgwit Address: {0}", segwitPublicAddress);
            Console.WriteLine("SegwitP2SH Address: {0}", segwitP2SHPublicAddress);
        }

        private void ReadConfigurations()
        {
            var appSettingFile = "appsettings.json";

            #if !DEBUG
            appSettingFile = "/settings/appsettings.json";
            #endif

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(appSettingFile, optional: true, reloadOnChange: false)
                .Build();

            this._pkDatabaseSettings = new PkDatabaseSettings();
            configuration.Bind("PkDatabaseSettings", this._pkDatabaseSettings);
        }
    }
}