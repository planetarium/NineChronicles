using System;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Ring : Equipment
    {
        public Ring(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data, id, requiredBlockIndex)
        {
        }

        public Ring(Dictionary serialized) : base(serialized)
        {
        }
    }
}
