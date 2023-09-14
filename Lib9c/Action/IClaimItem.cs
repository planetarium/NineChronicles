using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Action
{
    public interface IClaimItem
    {
        Address AvatarAddress { get; }
        FungibleAssetValue Amount { get; }
    }
}
