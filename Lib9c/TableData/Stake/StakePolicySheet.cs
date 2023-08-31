using System;
using System.Collections.Generic;

namespace Nekoyume.TableData.Stake
{
    public class StakePolicySheet : Sheet<string, StakePolicySheet.Row>
    {
        public static readonly (string attrName, string value)[] SheetPrefixRules =
        {
            ("StakeRegularRewardSheet", "StakeRegularRewardSheet_"),
            ("StakeRegularFixedRewardSheet", "StakeRegularFixedRewardSheet_"),
        };

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
                Validate();
            }

            public override void Validate()
            {
                foreach (var (attrName, value) in SheetPrefixRules)
                {
                    if (AttrName != attrName)
                    {
                        continue;
                    }

                    if (Value.StartsWith(value))
                    {
                        continue;
                    }

                    throw new SheetRowValidateException(
                        $"{nameof(Value)}({Value}) must start with" +
                        $" \"{value}\" when {nameof(AttrName)}({AttrName}) is" +
                        $" \"{attrName}\"");
                }
            }
        }

        public StakePolicySheet() : base(nameof(StakePolicySheet))
        {
        }
    }
}
