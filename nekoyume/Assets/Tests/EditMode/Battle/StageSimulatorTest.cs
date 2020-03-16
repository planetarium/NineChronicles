using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nekoyume;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Battle
{
    public class StageSimulatorTest
    {
        private TableSheets _tableSheets;

        [OneTimeSetUp]
        public void Init()
        {
            _tableSheets = new TableSheets();
            var request = Resources.Load<AddressableAssetsContainer>(Game.AddressableAssetsContainerPath);
            if (!(request is AddressableAssetsContainer addressableAssetsContainer))
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(Game.AddressableAssetsContainerPath);

            var csvAssets = addressableAssetsContainer.tableCsvAssets;
            foreach (var asset in csvAssets)
            {
                _tableSheets.SetToSheet(asset.name, asset.text);
            }

            _tableSheets.ItemSheetInitialize();
            _tableSheets.QuestSheetInitialize();

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
            Assert.AreEqual(new[]{306043, 303000}, reward.Select(i => i.Data.Id).ToArray());
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
            Assert.AreEqual(1, reward.Select(i => i.Data.Id).ToImmutableHashSet().Count);
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
    }
}
