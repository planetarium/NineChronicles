using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.Market;

namespace Nekoyume.Action
{
    public interface IRegisterInfo
    {
        public Address AvatarAddress { get; set; }
        public FungibleAssetValue Price { get; set; }
        public ProductType Type { get; set; }

        public IValue Serialize();
        public void ValidatePrice(Currency ncg);
        public void ValidateAddress(Address avatarAddress);
        public void Validate();
    }
}
