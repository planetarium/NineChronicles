using System;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Necklace : Equipment, ITradableItem
    {
        public Guid TradableId => ItemId;

        public Necklace(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex,
            bool madeWithMimisbrunnrRecipe = false) : base(data, id, requiredBlockIndex,
            madeWithMimisbrunnrRecipe)
        {
        }

        public Necklace(Dictionary serialized) : base(serialized)
        {
        }

        protected Necklace(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }
    }
}
