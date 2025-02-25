using System.Numerics;
using Libplanet.Types.Assets;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.TableData;

namespace Nekoyume.UI.Module
{
    public class MailReward
    {
        public ItemBase ItemBase { get; }
        public FungibleAssetValue FavFungibleAssetValue { get; }
        public BigInteger Count { get; }

        public bool IsPurchased { get; }

        public MailReward(ItemBase itemBase, BigInteger count, bool isPurchased = false)
        {
            ItemBase = itemBase;
            Count = count;
            IsPurchased = isPurchased;
        }

        public MailReward(FungibleAssetValue fav, BigInteger count, bool isPurchased = false)
        {
            FavFungibleAssetValue = fav;
            Count = count;
            IsPurchased = isPurchased;
        }
    }
}
