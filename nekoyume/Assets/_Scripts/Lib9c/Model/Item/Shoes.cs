using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Shoes : Equipment
    {
        public Shoes(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
        }
    }
}
