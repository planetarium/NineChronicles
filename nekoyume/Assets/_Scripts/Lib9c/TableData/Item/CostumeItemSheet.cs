using System;
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

            public Row() : base()
            {
            }

            public Row(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
            }

            public new static Row Deserialize(Bencodex.Types.Dictionary serialized)
            {
                return new Row(serialized);
            }
        }

        public CostumeItemSheet() :
            base(nameof(CostumeItemSheet))
        {
        }
    }
}
