using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Market;

namespace Nekoyume.Action
{
    public interface IRegisterInfo
    {
        public Address AvatarAddress { get; set; }
        public FungibleAssetValue Price { get; set; }
        public ProductType Type { get; set; }

        public IValue Serialize();
    }
}
