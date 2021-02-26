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

            public Row()
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

#pragma warning disable LAA1002
            public override IValue Serialize() => new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "spine_resource_path"] = (Text) SpineResourcePath,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                if (string.IsNullOrEmpty(fields[4]))
                {
                    switch (ItemSubType)
                    {
                        default:
                            SpineResourcePath = $"Character/PlayerSpineTexture/{ItemSubType}/{Id}";
                            break;
                        case ItemSubType.FullCostume:
                            SpineResourcePath = $"Character/{ItemSubType}/{Id}";
                            break;
                        case ItemSubType.Title:
                            SpineResourcePath = "";
                            break;
                    }
                }
                else
                {
                    SpineResourcePath = fields[4];
                }
            }
        }

        public CostumeItemSheet() :
            base(nameof(CostumeItemSheet))
        {
        }
    }
}
