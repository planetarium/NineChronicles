using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.Item;

namespace Nekoyume.TableData
{
    [Serializable]
    public class MaterialItemSheet : Sheet<int, MaterialItemSheet.Row>
    {
        [Serializable]
        public class Row : ItemSheet.Row, ISerializable
        {
            public HashDigest<SHA256> ItemId { get; private set; }
            public override ItemType ItemType => ItemType.Material;

            public Row() {}

            public Row(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                ItemId = Hashcash.Hash(serialized.EncodeIntoChunks().SelectMany(b => b).ToArray());
            }

            protected Row(SerializationInfo info, StreamingContext context)
                : this((Dictionary) new Codec().Decode((byte[])info.GetValue("encoded", typeof(byte[]))))
            {
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("encoded", new Codec().Encode(Serialize()));
            }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                ItemId = Hashcash.Hash(Serialize().EncodeIntoChunks().SelectMany(b => b).ToArray());
            }
        }

        public MaterialItemSheet() : base(nameof(MaterialItemSheet))
        {
        }
    }
}
