using System;
using Nekoyume.Model.Item;

namespace Nekoyume.TableData
{
    public class CostumeItemSheet : Sheet<int, CostumeItemSheet.Row>
    {
        [Serializable]
        public class Row : ItemSheet.Row
        {
            public override ItemType ItemType => ItemType.Costume;

            public Row() : base()
            {
            }
        }

        public CostumeItemSheet() :
            base(nameof(CostumeItemSheet))
        {
        }
    }
}
