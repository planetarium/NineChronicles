using System;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Necklace : Equipment
    {
        public Necklace(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
        }
    }
}
