using Libplanet.Crypto;

namespace Nekoyume.Blockchain
{
    public interface IAccessControlService
    {
        public int? GetTxQuota(Address address);
    }
}
