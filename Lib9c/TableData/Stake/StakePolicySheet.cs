using System;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.Model.Stake;

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

        public static readonly string[] RequiredAttrNames =
        {
            "StakeRegularFixedRewardSheet",
            "StakeRegularRewardSheet",
            "RewardInterval",
            "LockupInterval",
        };

        public static readonly (string attrName, string value)[] SheetPrefixRules =
        {
            ("StakeRegularFixedRewardSheet", Contract.StakeRegularFixedRewardSheetPrefix),
            ("StakeRegularRewardSheet", Contract.StakeRegularRewardSheetPrefix),
        };

        public string StakeRegularFixedRewardSheetValue =>
            this["StakeRegularFixedRewardSheet"].Value;

        public string StakeRegularRewardSheetValue =>
            this["StakeRegularRewardSheet"].Value;

        public long RewardIntervalValue =>
            long.Parse(this["RewardInterval"].Value, CultureInfo.InvariantCulture);

        public long LockupIntervalValue =>
            long.Parse(this["LockupInterval"].Value, CultureInfo.InvariantCulture);

        public StakePolicySheet() : base(nameof(StakePolicySheet))
        {
        }

        public override void Set(string csv, bool isReversed = false)
        {
            base.Set(csv, isReversed);
            foreach (var requiredAttrName in RequiredAttrNames)
            {
                if (ContainsKey(requiredAttrName))
                {
                    continue;
                }

                throw new SheetRowNotFoundException(
                    Name,
                    nameof(Row.AttrName),
                    requiredAttrName);
            }
        }
    }
}
