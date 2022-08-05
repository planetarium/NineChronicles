using System;

namespace Lib9c.DevExtensions.Model
{
    [Serializable]
    public class TestbedCreateAvatar : BaseTestbedModel
    {
        public long BlockDifficulty;
        public int TradableMaterialCount;
        public int MaterialCount;
        public int FoodCount;
        public CustomEquipmentItem[] CustomEquipmentItems;
    }

    [Serializable]
    public class CustomEquipmentItem
    {
        public int ID;
        public int Level;
        public int[] OptionIds;
    }
}
