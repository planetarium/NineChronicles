using System;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Weapon : Equipment
    {
        public Weapon(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data, id, requiredBlockIndex)
        {
        }

        public Weapon(Dictionary serialized) : base(serialized)
        {
        }
    }
}
