using System;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Necklace : Equipment
    {
        public Necklace(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data, id, requiredBlockIndex)
        {
        }

        public Necklace(Dictionary serialized) : base(serialized)
        {
        }
    }
}
