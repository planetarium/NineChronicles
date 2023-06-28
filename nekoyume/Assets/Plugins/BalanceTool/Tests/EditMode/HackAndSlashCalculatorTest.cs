using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BalanceTool.Runtime;
using BalanceTool.Runtime.Util.Lib9c.Tests.Util;
using Cysharp.Threading.Tasks;
using Libplanet;
using Libplanet.Action;
using Libplanet.State;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace BalanceTool.Tests.EditMode
{
    public class HackAndSlashCalculatorTest
    {
        private const string PlayDataCsv = @"
world_id,stage_id,avatar_level,equipment_00_id,equipment_00_level,equipment_01_id,equipment_01_level,equipment_02_id,equipment_02_level,equipment_03_id,equipment_03_level,equipment_04_id,equipment_04_level,equipment_05_id,equipment_05_level,food_00_id,food_00_count,food_01_id,food_01_count,food_02_id,food_02_count,food_03_id,food_03_count,costume_00_id,costume_01_id,costume_02_id,costume_03_id,costume_04_id,costume_05_id,rune_00_id,rune_00_level,rune_01_id,rune_01_level,rune_02_id,rune_02_level,rune_03_id,rune_03_level,crystal_random_buff_id,play_count
1,1,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,0,10
1,7,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,0,10
1,7,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,1,10
1,13,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,1,10";

        private const string ExpectedCsv = @"
world_id,stage_id,avatar_level,equipment_00_id,equipment_00_level,equipment_01_id,equipment_01_level,equipment_02_id,equipment_02_level,equipment_03_id,equipment_03_level,equipment_04_id,equipment_04_level,equipment_05_id,equipment_05_level,food_00_id,food_00_count,food_01_id,food_01_count,food_02_id,food_02_count,food_03_id,food_03_count,costume_00_id,costume_01_id,costume_02_id,costume_03_id,costume_04_id,costume_05_id,rune_00_id,rune_00_level,rune_01_id,rune_01_level,rune_02_id,rune_02_level,rune_03_id,rune_03_level,crystal_random_buff_id,play_count,wave_01_cleared,wave_02_cleared,wave_03_cleared,reward_00_id,reward_00_count,reward_01_id,reward_01_count,reward_02_id,reward_02_count,reward_03_id,reward_03_count,reward_04_id,reward_04_count,reward_05_id,reward_05_count,reward_06_id,reward_06_count,reward_07_id,reward_07_count,reward_08_id,reward_08_count,reward_09_id,reward_09_count,total_exp
1,1,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,0,10,10,10,10,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,150
1,7,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,0,10,10,6,0,303000,4,303100,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,600
1,7,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,1,10,10,10,10,303100,4,303000,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,600
1,13,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,1,10,6,2,0,303200,2,306023,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1800";

        private const string ExpectedCsv_GlobalPlayCount_1 = @"
world_id,stage_id,avatar_level,equipment_00_id,equipment_00_level,equipment_01_id,equipment_01_level,equipment_02_id,equipment_02_level,equipment_03_id,equipment_03_level,equipment_04_id,equipment_04_level,equipment_05_id,equipment_05_level,food_00_id,food_00_count,food_01_id,food_01_count,food_02_id,food_02_count,food_03_id,food_03_count,costume_00_id,costume_01_id,costume_02_id,costume_03_id,costume_04_id,costume_05_id,rune_00_id,rune_00_level,rune_01_id,rune_01_level,rune_02_id,rune_02_level,rune_03_id,rune_03_level,crystal_random_buff_id,play_count,wave_01_cleared,wave_02_cleared,wave_03_cleared,reward_00_id,reward_00_count,reward_01_id,reward_01_count,reward_02_id,reward_02_count,reward_03_id,reward_03_count,reward_04_id,reward_04_count,reward_05_id,reward_05_count,reward_06_id,reward_06_count,reward_07_id,reward_07_count,reward_08_id,reward_08_count,reward_09_id,reward_09_count,total_exp
1,1,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,0,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,15
1,7,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,60
1,7,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,1,1,1,1,1,303100,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,60
1,13,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,300";

        private IReadOnlyList<HackAndSlashCalculator.PlayData> _expectedPlayDataList;

        private IReadOnlyList<HackAndSlashCalculator.PlayData>
            _expectedPlayDataList_GlobalPlayCount_1;

        private IAccountStateDelta _prevStates;
        private Address _agentAddr;
        private int _avatarIndex;

        [SetUp]
        public void SetUp()
        {
            _expectedPlayDataList =
                HackAndSlashCalculator.ConvertToPlayDataList(ExpectedCsv);
            _expectedPlayDataList_GlobalPlayCount_1 =
                HackAndSlashCalculator.ConvertToPlayDataList(ExpectedCsv_GlobalPlayCount_1);

            (
                _,
                _agentAddr,
                _,
                _,
                _prevStates) = InitializeUtil.InitializeStates(
                avatarIndex: _avatarIndex);
            _avatarIndex = 0;
        }

        [UnityTest]
        public IEnumerator CalculateAsync()
        {
            yield return CalculateAsync(PlayDataCsv, 0).ToCoroutine();
            yield return CalculateAsync(PlayDataCsv, 1).ToCoroutine();
        }

        private async UniTask CalculateAsync(string playDataCsv, int globalPlayCount)
        {
            var playDataList = globalPlayCount == 0
                ? HackAndSlashCalculator.ConvertToPlayDataList(
                    playDataCsv)
                : HackAndSlashCalculator.ConvertToPlayDataList(
                    playDataCsv,
                    globalPlayCount: globalPlayCount);
            var playDataListWithResult = (await HackAndSlashCalculator.CalculateAsync(
                _prevStates,
                randomSeed: 0,
                0,
                _agentAddr,
                _avatarIndex,
                playDataList)).ToArray();
            Assert.AreEqual(4, playDataListWithResult.Length);
            for (var i = 0; i < playDataListWithResult.Length; i++)
            {
                var playData = playDataListWithResult[i];
                var expected = globalPlayCount == 0
                    ? _expectedPlayDataList[i]
                    : _expectedPlayDataList_GlobalPlayCount_1[i];
                Assert.AreEqual(expected.WorldId, playData.WorldId);
                Assert.AreEqual(expected.StageId, playData.StageId);
                Assert.AreEqual(expected.AvatarLevel, playData.AvatarLevel);
                Assert.IsTrue(expected.Equipments.SequenceEqual(playData.Equipments));
                Assert.IsTrue(expected.Foods.SequenceEqual(playData.Foods));
                Assert.IsTrue(expected.CostumeIds.SequenceEqual(playData.CostumeIds));
                Assert.IsTrue(expected.Runes.SequenceEqual(playData.Runes));
                Assert.AreEqual(expected.CrystalRandomBuffId, playData.CrystalRandomBuffId);
                Assert.AreEqual(expected.PlayCount, playData.PlayCount);
                Assert.IsTrue(playData.Result.HasValue);
                var result = playData.Result.Value;
                var expectedResult = expected.Result!.Value;
                Assert.IsTrue(expectedResult.ClearedWaves.SequenceEqual(result.ClearedWaves));
                Assert.IsTrue(expectedResult.TotalRewards.SequenceEqual(result.TotalRewards));
                Assert.AreEqual(expectedResult.TotalExp, result.TotalExp);
            }
        }
    }
}
