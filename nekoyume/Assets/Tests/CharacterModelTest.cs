using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests
{
    public class CharacterModelTest : PlayModeTest
    {
        private IRandom _random;
        private Player _player;

        [UnitySetUp]
        public IEnumerator CharacterModelSetup()
        {
            yield return SetUp();
            _random = new TestRandom();
            var address = new Address();
            var agentAddress = new Address();
            var avatarState = new AvatarState(address, agentAddress, 1);
            var simulator = new Simulator(_random, avatarState, new List<Consumable>(), 1);
            _player = simulator.Player;
            _player.InitAI();
        }

        [Test]
        public void MonsterSelectSkill()
        {
            var data = Game.instance.TableSheets.CharacterSheet.OrderedList.First(i => i.Id > 200000);
            var monster = new Monster(data, 1, _player);
            monster.InitAI();

            Assert.IsNotEmpty(monster.Skills);

            //Check selected skill is first
            var skill = monster.Skills.Select(_random);
            var id = monster.Skills.OrderBy(i => i.chance).First().skillRow.SkillEffectId;
            Assert.AreEqual(id, skill.effect.id);
        }

        [Test]
        public void PlayerSelectSkill()
        {
            Assert.AreEqual(1, _player.Skills.Count());
            foreach (var skillRow in Game.instance.TableSheets.SkillSheet)
            {
                var skill = SkillFactory.Get(skillRow, (int) 1.3m, .1m);
                _player.Skills.Add(skill);
            }
            Assert.AreEqual(1 + Game.instance.TableSheets.SkillSheet.Count, _player.Skills.Count());

            //Check selected skill is first
            var selected = _player.Skills.Select(_random);
            Assert.AreEqual(1, selected.effect.id);
        }

        [Test]
        public void CheckBuff()
        {
            _player.targets.Add(_player);
            var skill = _player.Skills.First();
            skill.buffs = skill.skillRow.GetBuffs().Select(BuffFactory.Get).ToList();
            Assert.AreEqual(2, skill.buffs.Count);
            Assert.AreEqual(0, _player.buffs.Count);
            skill.Use(_player);
            Assert.AreEqual(2, _player.buffs.Count);
            foreach (var pair in _player.buffs)
            {
                var playerBuff = pair.Value;
                var skillBuff = skill.buffs.First(i => i.Data.GroupId == pair.Key);
                Assert.AreEqual(playerBuff.remainedDuration, skillBuff.remainedDuration);
                playerBuff.remainedDuration--;
                Assert.Greater(skillBuff.remainedDuration, playerBuff.remainedDuration);
            }
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
