using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Necklace : Equipment
    {
        public Necklace(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
        }
    }
}
