namespace BitcoinPkCreatorWorker
{
    public class PublicAddress
    {
        public string Address { get; set; }
        public string Wif { get; set; }
        public string Type { get; set; }
        public DateTime? LastVerification { get; set; }
        public long Balance { get; set; }
        public int TransactionCount { get; set; }
    }
}