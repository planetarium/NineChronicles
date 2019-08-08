using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game;
using Nekoyume.Game.Entrance;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests
{
    public class CombinationTest
    {
        private MinerFixture _miner;

        [Test]
        public void GetEquipmentWithoutSkill()
        {
            var equipment = Tables.instance.ItemEquipment.First().Value;
            var parts = Tables.instance.Item.Select(i => i.Value).First(r => r.skillId == 0);

            var result = Nekoyume.Action.Combination.GetEquipment(equipment, parts, 0, default);
            Assert.NotNull(result);
            Assert.Null(result.SkillBase);
        }

        [Test]
        public void GetEquipmentWithSkill()
        {
            var equipment = Tables.instance.ItemEquipment.First().Value;
            var parts = Tables.instance.Item.Select(i => i.Value)
                .First(r => r.skillId != 0 && r.minChance > 0.01m);

            var result = Nekoyume.Action.Combination.GetEquipment(equipment, parts, 0, default);
            Assert.NotNull(result);
            Assert.AreEqual(parts.minChance, result.SkillBase.chance);
            Assert.AreEqual(parts.elemental, result.SkillBase.elementalType);
            Assert.AreEqual(parts.minDamage, result.SkillBase.power);
        }

        [UnityTest]
        public IEnumerator CombinationSuccess()
        {
            _miner = new MinerFixture("combination");

            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            Widget.Find<LoginDetail>().CreateClick();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Any());
            var createAvatarTx = AgentController.Agent.Transactions.First().Value;
            yield return _miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.currentAvatarState.Value is null);
            yield return new WaitUntil(() => Widget.Find<Login>().ready);

            // Login
            Widget.Find<Login>().SlotClick(2);
            Widget.Find<LoginDetail>().LoginClick();
            yield return new WaitUntil(() => GameObject.Find("room"));

            var w = Widget.Find<Combination>();
            w.Show();
            yield return new WaitUntil(() => w.isActiveAndEnabled);

            //Combination
            var row = Tables.instance.Recipe.Values.First();
            var rect = w.inventory.scrollerController.GetComponentInChildren<ScrollRect>();
            foreach (var material in new [] {row.Material1, row.Material2})
            {
                var index = States.Instance.currentAvatarState.Value.inventory.Items.ToList()
                    .FindIndex(i => i.item.Data.id == material);
                InventoryItemView item;
                while (true)
                {
                    item = w.inventory.scrollerController.GetByIndex(index);
                    if (item is null)
                    {
                        rect.GetComponent<ScrollRect>().verticalNormalizedPosition -= 0.1f;
                        yield return null;
                    }
                    else
                    {
                        break;
                    }

                }
                item.GetComponent<Button>().onClick.Invoke();
                w.inventory.Tooltip.submitButton.onClick.Invoke();
            }
            var current = AgentController.Agent.Transactions.Count;
            w.combinationButton.onClick.Invoke();
            yield return new WaitUntil(() => AgentController.Agent.Transactions.Count > current);
            var tx = AgentController.Agent.Transactions.Values.OrderByDescending(t => t.Timestamp).First();
            yield return _miner.CoMine(tx);
            yield return new WaitWhile(() => Widget.Find<GrayLoadingScreen>().gameObject.activeSelf);
            Assert.IsTrue(States.Instance.currentAvatarState.Value.inventory.Items.Select(i => i.item.Data.id)
                .Contains(row.ResultId));
        }

        [TearDown]
        public void TearDown()
        {
            _miner?.TearDown();
        }
    }
}
