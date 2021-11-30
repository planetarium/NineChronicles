using System;
using Nekoyume.Model.Item;

namespace Lib9c.DevExtensions.Model
{
    [Serializable]
    public class TestbedSell
    {
        public Avatar avatar;
        public Item[] Items;

        public TestbedSell()
        {
        }

        public TestbedSell(TestbedSell data)
        {
            avatar = data.avatar;
            Items = data.Items;
        }
    }

    [Serializable]
    public class Avatar
    {
        public string Name;
    }

    [Serializable]
    public class Item
    {
        public ItemSubType ItemSubType;
        public int ID;
        public int Level;
        public int Count;
        public int Price;
        public int[] OptionIds;
    }
}
