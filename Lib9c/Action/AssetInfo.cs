using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    public class AssetInfo: IRegisterInfo
    {
        public Address AvatarAddress { get; set; }
        public FungibleAssetValue Price { get; set; }
        public FungibleAssetValue Asset { get; set; }
        public ProductType Type { get; set; }

        public AssetInfo()
        {
        }

        public AssetInfo(List serialized)
        {
            AvatarAddress = serialized[0].ToAddress();
            Price = serialized[1].ToFungibleAssetValue();
            Type = serialized[2].ToEnum<ProductType>();
            Asset = serialized[3].ToFungibleAssetValue();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(AvatarAddress.Serialize())
                .Add(Price.Serialize())
                .Add(Type.Serialize())
                .Add(Asset.Serialize());
        }
    }
}
