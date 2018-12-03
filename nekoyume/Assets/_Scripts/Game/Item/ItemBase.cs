namespace Nekoyume.Game.Item
{
    public class ItemBase
    {
        public Data.Table.Item Data { get; private set; }

        public ItemBase(Data.Table.Item data)
        {
            Data = data;
        }
    }
}
