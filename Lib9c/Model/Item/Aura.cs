using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Aura : Equipment
    {
        public Aura(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex, bool madeWithMimisbrunnrRecipe = false) : base(data, id, requiredBlockIndex, madeWithMimisbrunnrRecipe)
        {
        }

        public Aura(Dictionary serialized) : base(serialized)
        {
        }

        protected Aura(SerializationInfo info, StreamingContext _) : base(info, _)
        {
        }
    }
}
