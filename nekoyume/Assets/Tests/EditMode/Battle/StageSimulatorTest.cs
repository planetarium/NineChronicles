using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Nekoyume;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NUnit.Framework;

namespace Tests.EditMode.Battle
{
    public class StageSimulatorTest
    {
        private TableSheets _tableSheets;
        private AgentState _agentState;
        private AvatarState _avatarState;

        [OneTimeSetUp]
        public void Init()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
            _agentState = new AgentState(new Address());
            _avatarState = new AvatarState(new Address(), _agentState.address, 0, _tableSheets, new GameConfigState());
        }

        [Test]
        public void SetRewardAll()
        {
            var row = _tableSheets.StageSheet.Values.First();
            row.Set(new List<string>
            {
                "1", "5", "100", "0", "0", "0", "0", "0", "0", "chapter_1_1", "bgm_stage_green", "306043", "1", "1",
                "1", "303000", "0.01", "1", "1", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
                "", "", "", "", "", "", "", "", "", "", "", "", "", "", "2", "2"
            });
            var reward = StageSimulator.SetReward(row, new Cheat.DebugRandom(), _tableSheets);
            Assert.AreEqual(2,reward.Count);
            Assert.IsNotEmpty(reward);
            Assert.AreEqual(new[]{306043, 303000}, reward.Select(i => i.Id).ToArray());
        }

        [Test]
        public void SetRewardDuplicateItem()
        {
            var row = _tableSheets.StageSheet.Values.First();
            row.Set(new List<string>
            {
                "1", "5", "100", "0", "0", "0", "0", "0", "0", "chapter_1_1", "bgm_stage_green", "306043", "1", "2",
                "2", "303000", "0.01", "2", "2", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
                "", "", "", "", "", "", "", "", "", "", "", "", "", "", "2", "2"
            });
            var reward = StageSimulator.SetReward(row, new Cheat.DebugRandom(), _tableSheets);
            Assert.AreEqual(2, reward.Count);
            Assert.IsNotEmpty(reward);
            Assert.AreEqual(1, reward.Select(i => i.Id).ToImmutableHashSet().Count);
        }

        [Test]
        public void SetRewardLimitByStageDrop()
        {
            var row = _tableSheets.StageSheet.Values.First();
            row.Set(new List<string>
            {
                "1", "5", "100", "0", "0", "0", "0", "0", "0", "chapter_1_1", "bgm_stage_green", "306043", "1", "2",
                "2", "303000", "0.01", "2", "2", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
                "", "", "", "", "", "", "", "", "", "", "", "", "", "", "1", "1"
            });
            var reward = StageSimulator.SetReward(row, new Cheat.DebugRandom(), _tableSheets);
            Assert.AreEqual(1, reward.Count);
        }

        [Test]
        public void SetRewardLimitByItemDrop()
        {
            var row = _tableSheets.StageSheet.Values.First();
            row.Set(new List<string>
            {
                "1", "5", "100", "0", "0", "0", "0", "0", "0", "chapter_1_1", "bgm_stage_green", "306043", "1", "1",
                "1", "303000", "0.01", "1", "1", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
                "", "", "", "", "", "", "", "", "", "", "", "", "", "", "1", "4"
            });
            var reward = StageSimulator.SetReward(row, new Cheat.DebugRandom(), _tableSheets);
            Assert.IsTrue(reward.Count <= 2);
            Assert.IsNotEmpty(reward);
        }

        [Test]
        public void IsClearBeforeSimulate()
        {
            var agentState = new AgentState(new Address());
            var avatarState = new AvatarState(new Address(), agentState.address, 0, _tableSheets, new GameConfigState());
            var simulator = new StageSimulator(new Cheat.DebugRandom(), avatarState, new List<Guid>(), 1, 1, _tableSheets);
            Assert.IsFalse(simulator.Log.IsClear);
        }

        [Test, Sequential]
        public void IsClear([Values(true, false)] bool expected, [Values(1, 10)] int stage)
        {
            var simulator = new StageSimulator(new Cheat.DebugRandom(), _avatarState, new List<Guid>(), 1, stage,
                _tableSheets);
            simulator.Simulate();
            Assert.AreEqual(BattleLog.Result.Win, simulator.Result);
            Assert.AreEqual(expected, simulator.Log.IsClear);
        }
    }
}
