using System.Security.Cryptography;
using Libplanet;

namespace Nekoyume.Model.Item
{
    public interface IFungibleItem: IItem
    {
        HashDigest<SHA256> FungibleId { get; }
    }
}
