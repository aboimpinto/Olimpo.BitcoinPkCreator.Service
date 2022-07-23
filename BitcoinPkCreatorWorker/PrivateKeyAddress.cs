using MongoDB.Bson.Serialization.Attributes;

namespace BitcoinPkCreatorWorker
{
    public class PrivateKeyAddress
    {
        [BsonElement("PrivateKeyBytes")]
        public byte[] PrivateKeyBytes { get; set; }
    }
}