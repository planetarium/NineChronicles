using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class SetItem : Equipment
    {
        public SetItem(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
        }

        public static Dictionary<int, int> WeaponMap =>
            new Dictionary<int, int>
            {
                [308001] = 301001,
                [308002] = 301002,
                [308003] = 301003,
            };
    }
}
