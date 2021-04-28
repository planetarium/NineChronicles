using System.Security.Cryptography;
using Libplanet;

namespace Nekoyume.Model.Item
{
    public interface IFungibleItem
    {
        HashDigest<SHA256> FungibleId { get; }
    }
}
