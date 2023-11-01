using Libplanet.Crypto;

namespace Nekoyume.Blockchain
{
    public interface IAccessControlService
    {
        public bool IsListed(Address address);

        public int? GetTxQuota(Address address);
    }
}
