using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;

namespace Nekoyume.TableData
{
    [Serializable]
    public class CostumeItemSheet : Sheet<int, CostumeItemSheet.Row>
    {
        [Serializable]
        public class Row : ItemSheet.Row
        {
            public override ItemType ItemType => ItemType.Costume;
            public string SpineResourcePath { get; private set; }

            public Row() : base()
            {
            }

            public Row(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                SpineResourcePath = (Text) serialized["spine_resource_path"];
            }

            public new static Row Deserialize(Bencodex.Types.Dictionary serialized)
            {
                return new Row(serialized);
            }

            public override IValue Serialize() => new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "spine_resource_path"] = (Text) SpineResourcePath,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                SpineResourcePath = string.IsNullOrEmpty(fields[4])
                    ? ItemSubType == ItemSubType.FullCostume
                        ? $"Character/FullCostume/{Id}"
                        : $"Character/PlayerSpineTexture/{ItemSubType}/{Id}"
                    : fields[4];
            }
        }

        public CostumeItemSheet() :
            base(nameof(CostumeItemSheet))
        {
        }
    }
}
