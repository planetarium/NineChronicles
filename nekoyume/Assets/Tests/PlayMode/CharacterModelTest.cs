using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Model;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.State;
using Nekoyume.TableData;
using NUnit.Framework;
using Tests.PlayMode.Fixtures;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class CharacterModelTest : PlayModeTest
    {
        private IRandom _random;
        private Player _player;

        [UnitySetUp]
        public IEnumerator CharacterModelSetup()
        {
            _random = new TestRandom();
            var address = new Address();
            var agentAddress = new Address();
            var avatarState = new AvatarState(address, agentAddress, 1, Game.instance.TableSheets);
            var simulator = new StageSimulator(_random, avatarState, new List<Consumable>(), 1, 1);
            _player = simulator.Player;
            _player.InitAI();
            yield return null;
        }

        [Test]
        public void MonsterSelectSkill()
        {
            var data = Game.instance.TableSheets.CharacterSheet.OrderedList.First(i => i.Id > 200000);
            var monster = new Enemy(_player, data, 1);
            monster.InitAI();

            Assert.IsNotEmpty(monster.Skills);

            //Check selected skill is first
            var skill = monster.Skills.Select(_random);
            var id = monster.Skills.OrderBy(i => i.chance).First().skillRow.Id;
            Assert.AreEqual(id, skill.skillRow.Id);
        }

        [Test]
        public void PlayerSelectSkill()
        {
            Assert.AreEqual(1, _player.Skills.Count());
            foreach (var skillRow in Game.instance.TableSheets.SkillSheet)
            {
                var skill = SkillFactory.Get(skillRow, (int) 1.3m, 10);
                _player.Skills.Add(skill);
            }
            Assert.AreEqual(1 + Game.instance.TableSheets.SkillSheet.Count, _player.Skills.Count());

            //Check selected skill is first
            var selected = _player.Skills.Select(_random);
            Assert.AreEqual(1, selected.skillRow.Id);
        }

        [Test]
        public void CheckBuff()
        {
            _player.Targets.Add(_player);
            var skill = _player.Skills.First();
            skill.buffs = skill.skillRow.GetBuffs().Select(BuffFactory.Get).ToList();
            Assert.AreEqual(0, _player.Buffs.Count);
            var model = skill.Use(_player, 0);
            if (model.BuffInfos is null)
                return;
            
            Assert.AreEqual(model.BuffInfos.Count(), _player.Buffs.Count);
                
            foreach (var pair in _player.Buffs)
            {
                var playerBuff = pair.Value;
                var skillBuff = skill.buffs.First(i => i.RowData.GroupId == pair.Key);
                Assert.AreEqual(playerBuff.remainedDuration, skillBuff.remainedDuration);
                playerBuff.remainedDuration--;
                Assert.Greater(skillBuff.remainedDuration, playerBuff.remainedDuration);
            }
        }

        [Test]
        public void StatsWithIntDebuff()
        {
            var row = Game.instance.TableSheets.BuffSheet.Values.First(i => i.Id == 202001);
            var debuff = BuffFactory.Get(row);
            var currentAtk = _player.ATK;
            Assert.AreEqual(9, currentAtk);
            _player.AddBuff(debuff);
            Assert.AreEqual(5, _player.ATK);

        }

        [Test]
        public void StatsWithMinusDebuff()
        {
            var row = Game.instance.TableSheets.BuffSheet.Values.First(i => i.Id == 202001);
            row.StatModifier.SetForTest(-200);
            var debuff = BuffFactory.Get(row);
            var currentAtk = _player.ATK;
            Assert.AreEqual(9, currentAtk);
            _player.AddBuff(debuff);
            Assert.AreEqual(0, _player.ATK);

        }

        [Test]
        public void StatsWithDecimalDebuff()
        {
            var row = Game.instance.TableSheets.BuffSheet.Values.First(i => i.Id == 204001);
            var debuff = BuffFactory.Get(row);
            var currentCri = _player.CRI;
            Assert.AreEqual(7, currentCri);
            _player.AddBuff(debuff);
            Assert.AreEqual(3, _player.CRI);

        }

        [Test]
        public void StatsWithMinusDecimalDebuff()
        {
            var row = Game.instance.TableSheets.BuffSheet.Values.First(i => i.Id == 204001);
            row.StatModifier.SetForTest(-200);
            var debuff = BuffFactory.Get(row);
            var currentCri = _player.CRI;
            Assert.AreEqual(7, currentCri);
            _player.AddBuff(debuff);
            Assert.AreEqual(0, _player.CRI);

        }
        private class TestRandom : IRandom
        {
            public int Next()
            {
                return 0;
            }

            public int Next(int maxValue)
            {
                return 0;
            }

            public int Next(int minValue, int maxValue)
            {
                return 0;
            }

            public void NextBytes(byte[] buffer)
            {
            }

            public double NextDouble()
            {
                return default;
            }
        }
    }
}
