using System.Linq;
using Libplanet.Crypto;
using Nekoyume.Helper;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.TableData;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="InfiniteTowerFloorHelper"/>: a season can keep its floor rows in its own id range
    /// (FloorBegin~FloorEnd) while the on-screen floor (Floor column) stays 1-based.
    /// </summary>
    public class InfiniteTowerFloorHelperTest
    {
        private const string FloorHeader =
            "Id,Floor,RequiredCp,MaxCp,ForbiddenItemSubTypes,MinItemGrade,MaxItemGrade,MinItemLevel,MaxItemLevel,GuaranteedConditionId,MinRandomConditions,MaxRandomConditions,RandomConditionId1,RandomConditionWeight1,RandomConditionId2,RandomConditionWeight2,RandomConditionId3,RandomConditionWeight3,RandomConditionId4,RandomConditionWeight4,RandomConditionId5,RandomConditionWeight5,ItemRewardId1,ItemRewardCount1,ItemRewardId2,ItemRewardCount2,ItemRewardId3,ItemRewardCount3,ItemRewardId4,ItemRewardCount4,ItemRewardId5,ItemRewardCount5,FungibleAssetRewardTicker1,FungibleAssetRewardAmount1,FungibleAssetRewardTicker2,FungibleAssetRewardAmount2,FungibleAssetRewardTicker3,FungibleAssetRewardAmount3,FungibleAssetRewardTicker4,FungibleAssetRewardAmount4,FungibleAssetRewardTicker5,FungibleAssetRewardAmount5,NcgCost,MaterialCostId,MaterialCostCount,ForbiddenRuneTypes,RequiredElementalType,hp_option,atk_option,def_option,cri_option,hit_option,spd_option";

        // Everything after "Id,Floor," of a real floor row.
        private const string FloorRowTail =
            "1500000,,,,,,,38,1,2,12,80,42,50,46,20,49,15,,,500001,100,600306,100,,,,,,,,,,,,,,,,,120,600206,30,1,0:1:3,10000,5000,10000,100,10000,5000";

        private InfiniteTowerFloorSheet _floorSheet;

        [SetUp]
        public void SetUp()
        {
            // Season A uses ids 1~2, season B uses ids 121~122; both show floors 1~2.
            var rows = new[] { (1, 1), (2, 2), (121, 1), (122, 2) }
                .Select(r => $"{r.Item1},{r.Item2},{FloorRowTail}");
            _floorSheet = new InfiniteTowerFloorSheet();
            _floorSheet.Set(string.Join("\n", new[] { FloorHeader }.Concat(rows)));
        }

        [Test]
        public void FindFloorRow_UsesCurrentSeasonRange()
        {
            Assert.AreEqual(1, InfiniteTowerFloorHelper.FindFloorRow(_floorSheet, Schedule(1, 1, 120), 1).Id);
            Assert.AreEqual(122, InfiniteTowerFloorHelper.FindFloorRow(_floorSheet, Schedule(2, 121, 240), 2).Id);
            Assert.IsNull(InfiniteTowerFloorHelper.FindFloorRow(_floorSheet, Schedule(2, 121, 240), 3));
        }

        [Test]
        public void GetFloorNumber_ReturnsFloorColumn()
        {
            Assert.AreEqual(2, InfiniteTowerFloorHelper.GetFloorNumber(_floorSheet, 122));
            Assert.AreEqual(999, InfiniteTowerFloorHelper.GetFloorNumber(_floorSheet, 999));
        }

        [Test]
        public void GetClearedFloorNumber_ConvertsCurrentSeasonOnly()
        {
            var seasonB = Schedule(2, 121, 240);

            Assert.AreEqual(0, InfiniteTowerFloorHelper.GetClearedFloorNumber(_floorSheet, seasonB, Info(2, 0)));
            Assert.AreEqual(1, InfiniteTowerFloorHelper.GetClearedFloorNumber(_floorSheet, seasonB, Info(2, 121)));
            // Info left over from the previous season (other tower id, or a floor outside this range).
            Assert.AreEqual(0, InfiniteTowerFloorHelper.GetClearedFloorNumber(_floorSheet, seasonB, Info(1, 2)));
            Assert.AreEqual(0, InfiniteTowerFloorHelper.GetClearedFloorNumber(_floorSheet, seasonB, Info(2, 2)));
            // Without a schedule row the cleared floor is shown as is.
            Assert.AreEqual(2, InfiniteTowerFloorHelper.GetClearedFloorNumber(_floorSheet, null, Info(1, 2)));
        }

        private static InfiniteTowerScheduleSheet.Row Schedule(int towerId, int floorBegin, int floorEnd)
        {
            var row = new InfiniteTowerScheduleSheet.Row();
            row.Set(new[]
            {
                towerId.ToString(), towerId.ToString(), "0", "1000000", "1", "10", "3600",
                floorBegin.ToString(), floorEnd.ToString(),
            });
            return row;
        }

        private static InfiniteTowerInfo Info(int towerId, int clearedFloor)
        {
            var info = new InfiniteTowerInfo(new PrivateKey().Address, towerId, 1);
            info.ClearFloor(clearedFloor);
            return info;
        }
    }
}
