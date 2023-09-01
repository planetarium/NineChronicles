namespace Lib9c.Tests.TableData.Stake
{
    using System.Linq;
    using System.Text;
    using Lib9c.Tests.Fixtures.TableCSV.Stake;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Stake;
    using Xunit;

    public class StakePolicySheetTest
    {
        [Fact]
        public void Set_Success()
        {
            var sheet = new StakePolicySheet();
            sheet.Set(StakePolicySheetFixtures.V1);
            Assert.Equal(4, sheet.Count);
            var row = sheet["StakeRegularFixedRewardSheet"];
            Assert.Equal("StakeRegularFixedRewardSheet", row.AttrName);
            Assert.Equal("StakeRegularFixedRewardSheet_V1", row.Value);
            row = sheet["StakeRegularRewardSheet"];
            Assert.Equal("StakeRegularRewardSheet", row.AttrName);
            Assert.Equal("StakeRegularRewardSheet_V1", row.Value);
            row = sheet["RewardInterval"];
            Assert.Equal("RewardInterval", row.AttrName);
            Assert.Equal("50400", row.Value);
            row = sheet["LockupInterval"];
            Assert.Equal("LockupInterval", row.AttrName);
            Assert.Equal("201600", row.Value);
        }

        [Theory]
        [InlineData("StakeRegularFixedRewardSheet,")]
        [InlineData("StakeRegularFixedRewardSheet,StakeRegularFixedRewardSheet")]
        [InlineData("StakeRegularFixedRewardSheet,StakeRegularRewardSheet_")]
        public void Set_Throw_SheetRowValidateException(string row)
        {
            var sb = new StringBuilder();
            sb.AppendLine("attr_name,value");
            sb.AppendLine(row);
            Assert.Throws<SheetRowValidateException>(() =>
                new StakePolicySheet().Set(sb.ToString()));
        }

        [Fact]
        public void Set_Throw_SheetRowNotFoundException()
        {
            foreach (var requiredAttrName in StakePolicySheet.RequiredAttrNames)
            {
                var csv = string.Join(
                    "\n",
                    StakePolicySheetFixtures.V1.Split("\n")
                        .Where(r => !r.StartsWith(requiredAttrName)));
                Assert.Throws<SheetRowNotFoundException>(() => new StakePolicySheet().Set(csv));
            }
        }
    }
}
