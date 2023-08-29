using System;
using System.Collections.Generic;

namespace Nekoyume.TableData.Stake
{
    public class StakePolicySheet : Sheet<string, StakePolicySheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<string>
        {
            public override string Key => TableTypeName;

            public string TableTypeName { get; private set; }

            public string TableName { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                TableTypeName = fields[0];
                TableName = fields[1];
            }
        }

        public StakePolicySheet() : base(nameof(StakePolicySheet))
        {
        }
    }
}
