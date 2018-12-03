namespace Nekoyume.Game.Item
{
    public class ItemUsable : ItemBase
    {
        public ItemUsable(Data.Table.Item data)
            : base(data)
        {

        }

        public virtual bool Use()
        {
            return false;
        }
    }
}
