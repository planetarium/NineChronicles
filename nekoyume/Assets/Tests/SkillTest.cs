using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume;
using Nekoyume.Battle;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;
using NUnit.Framework;

namespace Tests
{
    public class SkillTest
    {
        private Simulator _simulator;

        [SetUp]
        public void Setup()
        {
            var random = new Cheat.DebugRandom();
            var address = new Address();
            var agentAddress = new Address();
            var avatarState = new AvatarState(address, agentAddress);

            _simulator = new Simulator(random, avatarState, new List<Food>(), 1);
            var caster = _simulator.Player;
            var target = (CharacterBase) caster.Clone();
            caster.InitAI();
            caster.targets.Add(target);
            target.def = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _simulator = null;
        }

        [Test]
        public void Attack()
        {
            var caster = _simulator.Player;
            var attack = caster.Skills.First(s => s is Nekoyume.Game.Skill.NormalAttack);
            var result = attack.Use(caster);
            var target = caster.targets.First();

            Assert.AreEqual(target.hp - caster.atk, target.currentHP);
            Assert.AreEqual(1, result.skillInfos.Count());
            var info = result.skillInfos.First();
            Assert.AreEqual(caster.atk, info.Effect);
            Assert.NotNull(info.Target);
            Assert.AreEqual(SkillCategory.Normal, info.skillCategory);
            Assert.AreEqual(Elemental.ElementalType.Normal, info.Elemental);
        }

        [Test]
        public void Blow()
        {
            var caster = _simulator.Player;
            var effect = Tables.instance.SkillEffect.Values.First(r => r.skillCategory == SkillCategory.Blow);
            var blow = new Nekoyume.Game.Skill.BlowAttack(1, effect, Elemental.ElementalType.Normal, caster.atk);
            var result = blow.Use(caster);
            var target = caster.targets.First();

            Assert.AreEqual(target.hp - caster.atk, target.currentHP);
            Assert.AreEqual(1, result.skillInfos.Count());
            var info = result.skillInfos.First();
            Assert.AreEqual(caster.atk, info.Effect);
            Assert.NotNull(info.Target);
            Assert.AreEqual(SkillCategory.Blow, info.skillCategory);
            Assert.AreEqual(Elemental.ElementalType.Normal, info.Elemental);
        }

        [Test]
        public void DoubleAttack()
        {
            var caster = _simulator.Player;
            var effect = Tables.instance.SkillEffect.Values.First(r => r.skillCategory == SkillCategory.Double);
            var doubleAttack = new Nekoyume.Game.Skill.DoubleAttack(1, effect, Elemental.ElementalType.Normal, caster.atk);
            var result = doubleAttack.Use(caster);
            var target = caster.targets.First();

            Assert.AreEqual(target.hp - caster.atk, target.currentHP);
            Assert.AreEqual(2, result.skillInfos.Count());
            Assert.AreEqual(caster.atk, result.skillInfos.Sum(i => i.Effect));
            foreach (var info in result.skillInfos)
            {
                Assert.NotNull(info.Target);
                Assert.AreEqual(SkillCategory.Double, info.skillCategory);
                Assert.AreEqual(Elemental.ElementalType.Normal, info.Elemental);
            }
        }

        [Test]
        public void AreaAttack()
        {
            var caster = _simulator.Player;
            var effect = Tables.instance.SkillEffect.Values.First(r => r.skillCategory == SkillCategory.Area);
            var area = new Nekoyume.Game.Skill.AreaAttack(1, effect, Elemental.ElementalType.Normal, caster.atk);
            var result = area.Use(caster);
            var target = caster.targets.First();

            Assert.AreEqual(target.hp - caster.atk, target.currentHP);
            Assert.AreEqual(effect.hitCount, result.skillInfos.Count());
            Assert.LessOrEqual(result.skillInfos.Sum(i => i.Effect), caster.atk);
            foreach (var info in result.skillInfos)
            {
                Assert.NotNull(info.Target);
                Assert.AreEqual(SkillCategory.Area, info.skillCategory);
                Assert.AreEqual(Elemental.ElementalType.Normal, info.Elemental);
            }
        }

        [Test]
        public void Heal()
        {
            var caster = _simulator.Player;
            var effect = Tables.instance.SkillEffect.Values.First(r => r.type == SkillType.Buff);
            var heal = new Nekoyume.Game.Skill.Heal(1, effect, caster.atk);
            caster.currentHP -= caster.atk;
            var result = heal.Use(caster);

            Assert.AreEqual(caster.currentHP, caster.hp);
            Assert.AreEqual(1, result.skillInfos.Count());
            var info = result.skillInfos.First();
            Assert.AreEqual(caster.atk, info.Effect);
            Assert.NotNull(info.Target);
            Assert.AreEqual(1, result.skillInfos.Count());
            Assert.AreEqual(SkillCategory.Normal, info.skillCategory);
            Assert.Null(info.Elemental);
        }
    }
}
