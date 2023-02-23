using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    public class RegisterInfo: IRegisterInfo
    {
        public Address AvatarAddress { get; set; }
        public FungibleAssetValue Price { get; set; }
        public Guid TradableId { get; set; }
        public int ItemCount { get; set; }
        public ProductType Type { get; set; }

        public RegisterInfo(List serialized)
        {
            AvatarAddress = serialized[0].ToAddress();
            Price = serialized[1].ToFungibleAssetValue();
            Type = serialized[2].ToEnum<ProductType>();
            TradableId = serialized[3].ToGuid();
            ItemCount = serialized[4].ToInteger();
        }

        public RegisterInfo()
        {
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(AvatarAddress.Serialize())
                .Add(Price.Serialize())
                .Add(Type.Serialize())
                .Add(TradableId.Serialize())
                .Add(ItemCount.Serialize());
        }
    }
}
