namespace BitcoinPkCreatorWorker.Extentions
{
    public static class ByteArrayExtensions
    {
        public static string ToDescription(this byte[] byteArray)
        {
            return string.Join(", ", byteArray);
        }
    }
}