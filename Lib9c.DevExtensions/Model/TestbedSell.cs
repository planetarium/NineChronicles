using System;
using Nekoyume.Model.Item;

namespace Lib9c.DevExtensions.Model
{
    [Serializable]
    public class TestbedSell : BaseTestbedModel
    {
        public Avatar Avatar;
        public Item[] Items;
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
