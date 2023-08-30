using System;
using System.Collections.Generic;

namespace Nekoyume.TableData.Stake
{
    public class StakePolicySheet : Sheet<string, StakePolicySheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<string>
        {
            public override string Key => AttrName;

            public string AttrName { get; private set; }
            public string Value { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                AttrName = fields[0];
                Value = fields[1];
            }
        }

        public StakePolicySheet() : base(nameof(StakePolicySheet))
        {
        }
    }
}
