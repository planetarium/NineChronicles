using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Costume : ItemBase
    {
        public bool equipped = false;
        public string SpineResourcePath { get; }

        public Guid ItemId { get; }

        public Costume(CostumeItemSheet.Row data, Guid id) : base(data)
        {
            SpineResourcePath = data.SpineResourcePath;
            ItemId = id;
        }

        public Costume(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "equipped", out var toEquipped))
            {
                equipped = toEquipped.ToBoolean();
            }
            if (serialized.TryGetValue((Text) "spine_resource_path", out var spineResourcePath))
            {
                SpineResourcePath = (Text) spineResourcePath;
            }

            ItemId = serialized["item_id"].ToGuid();
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "equipped"] = equipped.Serialize(),
                [(Text) "spine_resource_path"] = SpineResourcePath.Serialize(),
                [(Text) "item_id"] = ItemId.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
