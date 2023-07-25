using Libplanet.Types.Assets;
using Nekoyume.Model.Item;

namespace Nekoyume.UI.Module
{
    public class MailReward
    {
        public ItemBase ItemBase { get; }
        public FungibleAssetValue FavFungibleAssetValue { get; }
        public int Count { get; }

        public bool IsPurchased { get; }

        public MailReward(ItemBase itemBase, int count, bool isPurchased = false)
        {
            ItemBase = itemBase;
            Count = count;
            IsPurchased = isPurchased;
        }

        public MailReward(FungibleAssetValue fav, int count, bool isPurchased = false)
        {
            FavFungibleAssetValue = fav;
            Count = count;
            IsPurchased = isPurchased;
        }
    }
}
