using System.Reactive.Subjects;

namespace BitcoinPkCreatorWorker
{
    public interface IPublicKeyCreatorService
    {
        Subject<PublicAddress> OnNewPublicAddress { get; }

        void CreatePublicKeys(byte[] souce);
    }
}