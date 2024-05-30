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

        public MailReward(int itemId, int count, bool isPurchased = false)
        {
            var itemRow = TableSheets.Instance.ItemSheet[itemId];
            if (itemRow is MaterialItemSheet.Row materialRow)
            {
                ItemBase = ItemFactory.CreateMaterial(materialRow);
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    if (itemRow.ItemSubType != ItemSubType.Aura)
                    {
                        ItemBase = ItemFactory.CreateItem(itemRow, new ActionRenderHandler.LocalRandom(0));
                    }
                }
            }
            Count = count;
            IsPurchased = isPurchased;
        }

        public MailReward(string ticker, int count, bool isPurchased = false)
        {
            var currency = Currency.Legacy(ticker, 0, null);
            FavFungibleAssetValue = new FungibleAssetValue(currency, count, 0);
            Count = count;
            IsPurchased = isPurchased;
        }
    }
}
