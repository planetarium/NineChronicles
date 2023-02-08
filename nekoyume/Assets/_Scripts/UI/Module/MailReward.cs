using Nekoyume.Model.Item;

namespace Nekoyume.UI.Module
{
    public class MailReward
    {
        public ItemBase ItemBase { get; }
        public int Count { get; }

        public bool IsPurchased { get; }

        public MailReward(ItemBase itemBase, int count, bool isPurchased = false)
        {
            ItemBase = itemBase;
            Count = count;
            IsPurchased = isPurchased;
        }
    }
}
