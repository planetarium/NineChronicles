using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Market
{
    public class ItemProduct : Product
    {
        public ITradableItem TradableItem;
        public int ItemCount;

        public ItemProduct()
        {
        }

        public ItemProduct(List serialized) : base(serialized)
        {
            TradableItem = (ITradableItem) ItemFactory.Deserialize((Dictionary)serialized[6]);
            ItemCount = serialized[7].ToInteger();
        }

        public override IValue Serialize()
        {
            List serialized = (List) base.Serialize();
            return serialized
                .Add(TradableItem.Serialize())
                .Add(ItemCount.Serialize());
        }
    }
}
