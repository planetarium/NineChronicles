using System.Security.Cryptography;
using Libplanet;

namespace Nekoyume.Model.Item
{
    public interface IFungibleItem: ITradableItem
    {
        HashDigest<SHA256> FungibleId { get; }

        bool IsTradable { get; }
    }
}
