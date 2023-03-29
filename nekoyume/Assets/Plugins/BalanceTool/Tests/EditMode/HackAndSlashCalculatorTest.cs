using System.Collections.Generic;
using System.Linq;
using BalanceTool.Runtime;
using BalanceTool.Runtime.Util.Lib9c.Tests.Util;
using Lib9c.DevExtensions;
using Libplanet;
using Libplanet.Action;
using NUnit.Framework;

namespace BalanceTool.Tests.EditMode
{
    public class HackAndSlashCalculatorTest
    {
        private const string PlayDataCsv =
            @"world_id,stage_id,avatar_level,equipment_00_id,equipment_00_level,equipment_01_id,equipment_01_level,equipment_02_id,equipment_02_level,equipment_03_id,equipment_03_level,equipment_04_id,equipment_04_level,equipment_05_id,equipment_05_level,food_00_id,food_00_count,food_01_id,food_01_count,food_02_id,food_02_count,food_03_id,food_03_count,costume_00_id,costume_01_id,costume_02_id,costume_03_id,costume_04_id,costume_05_id,rune_00_id,rune_00_level,rune_01_id,rune_01_level,rune_02_id,rune_02_level,rune_03_id,rune_03_level,play_count
              1,1,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,10
              1,6,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,10";

        private const string ExpectedCsv =
            @"world_id,stage_id,avatar_level,equipment_00_id,equipment_00_level,equipment_01_id,equipment_01_level,equipment_02_id,equipment_02_level,equipment_03_id,equipment_03_level,equipment_04_id,equipment_04_level,equipment_05_id,equipment_05_level,food_00_id,food_00_count,food_01_id,food_01_count,food_02_id,food_02_count,food_03_id,food_03_count,costume_00_id,costume_01_id,costume_02_id,costume_03_id,costume_04_id,costume_05_id,rune_00_id,rune_00_level,rune_01_id,rune_01_level,rune_02_id,rune_02_level,rune_03_id,rune_03_level,play_count,wave_01_cleared,wave_02_cleared,wave_03_cleared,reward_00_id,reward_00_count,reward_01_id,reward_01_count,reward_02_id,reward_02_count,reward_03_id,reward_03_count,reward_04_id,reward_04_count,reward_05_id,reward_05_count,reward_06_id,reward_06_count,reward_07_id,reward_07_count,reward_08_id,reward_08_count,reward_09_id,reward_09_count,total_exp
              1,1,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,10,10,10,10,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,150
              1,6,1,10100000,0,0,0,0,0,0,0,0,0,0,0,200000,1,0,0,0,0,0,0,40100000,0,0,0,0,0,30001,0,0,0,0,0,0,0,10,10,10,0,303100,10,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,500";

        private IReadOnlyList<HackAndSlashCalculator.PlayData> _expectedPlayDataList;

        private IAccountStateDelta _prevStates;
        private IRandom _random;
        private Address _agentAddr;
        private int _avatarIndex;

        [SetUp]
        public void SetUp()
        {
            _expectedPlayDataList = HackAndSlashCalculator.ConvertToPlayDataList(ExpectedCsv);

            (
                _,
                _agentAddr,
                _,
                _,
                _prevStates) = InitializeUtil.InitializeStates(
                avatarIndex: _avatarIndex);
            _random = new RandomImpl();
            _avatarIndex = 0;
        }

        [TestCase(PlayDataCsv, 0)]
        [TestCase(PlayDataCsv, 1)]
        public void Calculate(string playDataCsv, int globalPlayCount)
        {
            var playDataList = globalPlayCount > 0
                ? HackAndSlashCalculator.ConvertToPlayDataList(
                    playDataCsv,
                    globalPlayCount: globalPlayCount)
                : HackAndSlashCalculator.ConvertToPlayDataList(
                    playDataCsv);
            var playDataListWithResult = HackAndSlashCalculator.Calculate(
                _prevStates,
                _random,
                0,
                _agentAddr,
                _avatarIndex,
                playDataList).ToArray();
            Assert.AreEqual(2, playDataListWithResult.Length);
            var playData = playDataListWithResult[0];
            Assert.AreEqual(1, playData.WorldId);
            Assert.AreEqual(1, playData.StageId);
            Assert.AreEqual(1, playData.AvatarLevel);
            Assert.AreEqual(1, playData.Equipments.Length);
            Assert.AreEqual(10100000, playData.Equipments[0].equipmentId);
            Assert.AreEqual(0, playData.Equipments[0].level);
            Assert.AreEqual(1, playData.Foods.Length);
            Assert.AreEqual(200000, playData.Foods[0].consumableId);
            Assert.AreEqual(1, playData.Foods[0].count);
            Assert.AreEqual(1, playData.CostumeIds.Length);
            Assert.AreEqual(40100000, playData.CostumeIds[0]);
            Assert.AreEqual(1, playData.Runes.Length);
            Assert.AreEqual(0, playData.Runes[0].runeSlotIndex);
            Assert.AreEqual(30001, playData.Runes[0].runeId);
            Assert.AreEqual(0, playData.Runes[0].level);
            var expectedPlayCount = globalPlayCount > 0
                ? globalPlayCount
                : _expectedPlayDataList[0].PlayCount;
            Assert.AreEqual(expectedPlayCount, playData.PlayCount);
            Assert.IsTrue(playData.Result.HasValue);
            var result = playData.Result.Value;
            Assert.AreEqual(3, result.ClearedWaves.Count);
            Assert.AreEqual(expectedPlayCount, result.ClearedWaves[1]);
            Assert.AreEqual(expectedPlayCount, result.ClearedWaves[2]);
            Assert.AreEqual(expectedPlayCount, result.ClearedWaves[3]);
            Assert.AreEqual(0, result.TotalRewards.Count);
            Assert.AreEqual(expectedPlayCount * 15, result.TotalExp);

            playData = playDataListWithResult[1];
            Assert.AreEqual(1, playData.WorldId);
            Assert.AreEqual(6, playData.StageId);
            Assert.AreEqual(1, playData.AvatarLevel);
            Assert.AreEqual(1, playData.Equipments.Length);
            Assert.AreEqual(10100000, playData.Equipments[0].equipmentId);
            Assert.AreEqual(0, playData.Equipments[0].level);
            Assert.AreEqual(1, playData.Foods.Length);
            Assert.AreEqual(200000, playData.Foods[0].consumableId);
            Assert.AreEqual(1, playData.Foods[0].count);
            Assert.AreEqual(1, playData.CostumeIds.Length);
            Assert.AreEqual(40100000, playData.CostumeIds[0]);
            Assert.AreEqual(1, playData.Runes.Length);
            Assert.AreEqual(0, playData.Runes[0].runeSlotIndex);
            Assert.AreEqual(30001, playData.Runes[0].runeId);
            Assert.AreEqual(0, playData.Runes[0].level);
            expectedPlayCount = globalPlayCount > 0
                ? globalPlayCount
                : _expectedPlayDataList[1].PlayCount;
            Assert.AreEqual(expectedPlayCount, playData.PlayCount);
            Assert.IsTrue(playData.Result.HasValue);
            result = playData.Result.Value;
            Assert.AreEqual(2, result.ClearedWaves.Count);
            Assert.AreEqual(expectedPlayCount, result.ClearedWaves[1]);
            Assert.AreEqual(expectedPlayCount, result.ClearedWaves[2]);
            Assert.AreEqual(1, result.TotalRewards.Count);
            Assert.AreEqual(expectedPlayCount, result.TotalRewards[303100]);
            Assert.AreEqual(expectedPlayCount * 50, result.TotalExp);
        }
    }
}
