using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume;
using Nekoyume.Battle;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;
using Nekoyume.Game;
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
            var avatarState = new AvatarState(address, agentAddress, 1, 20);

            _simulator = new Simulator(random, avatarState, new List<Consumable>(), 1);
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
        public void NormalAttack()
        {
            var caster = _simulator.Player;
            var attack = caster.Skills.First(s => s is NormalAttack);
            var result = attack.Use(caster);
            var target = caster.targets.First();
            var info = result.skillInfos.First();
            Assert.AreEqual(target.currentHP, target.hp - info.Effect);
            Assert.AreEqual(1, result.skillInfos.Count());
            Assert.NotNull(info.Target);
            Assert.AreEqual(SkillCategory.Normal, info.skillCategory);
            Assert.AreEqual(ElementalType.Normal, info.Elemental);
        }

        [Test]
        public void BlowAttack()
        {
            var caster = _simulator.Player;
            var skillRow = ATestSetUp.tableSheets.SkillSheet.OrderedList.First(r =>
            {
                if (!Tables.instance.SkillEffect.TryGetValue(r.SkillEffectId, out var skillEffectRow))
                {
                    throw new KeyNotFoundException(nameof(r.SkillEffectId));
                }
                
                return skillEffectRow.skillCategory == SkillCategory.Blow;
            });
            var blow = new BlowAttack(skillRow, caster.atk, 1m);
            var result = blow.Use(caster);
            var target = caster.targets.First();
            var info = result.skillInfos.First();
            var atk = caster.atk + blow.power;
            if (info.Critical)
                atk = (int) (atk * CharacterBase.CriticalMultiplier);
            Assert.AreEqual(atk, info.Effect);
            Assert.AreEqual(target.currentHP, target.hp - info.Effect);
            Assert.AreEqual(1, result.skillInfos.Count());
            Assert.NotNull(info.Target);
            Assert.AreEqual(SkillCategory.Blow, info.skillCategory);
            Assert.AreEqual(ElementalType.Normal, info.Elemental);
        }

        [Test]
        public void DoubleAttack()
        {
            var caster = _simulator.Player;
            var skillRow = ATestSetUp.tableSheets.SkillSheet.OrderedList.First(r =>
            {
                if (!Tables.instance.SkillEffect.TryGetValue(r.SkillEffectId, out var skillEffectRow))
                {
                    throw new KeyNotFoundException(nameof(r.SkillEffectId));
                }
                
                return skillEffectRow.skillCategory == SkillCategory.Double;
            });
            var doubleAttack = new Nekoyume.Game.DoubleAttack(skillRow, caster.atk, 1m);
            var result = doubleAttack.Use(caster);
            var target = caster.targets.First();

            Assert.AreEqual(target.currentHP, target.hp - result.skillInfos.Sum(i => i.Effect));
            Assert.AreEqual(2, result.skillInfos.Count());
            foreach (var info in result.skillInfos)
            {
                Assert.NotNull(info.Target);
                Assert.AreEqual(SkillCategory.Double, info.skillCategory);
                Assert.AreEqual(ElementalType.Normal, info.Elemental);
            }
        }

        [Test]
        public void AreaAttack()
        {
            var caster = _simulator.Player;
            SkillEffect skillEffectRow = null;
            var skillRow = ATestSetUp.tableSheets.SkillSheet.OrderedList.First(r =>
            {
                if (!Tables.instance.SkillEffect.TryGetValue(r.SkillEffectId, out skillEffectRow))
                {
                    throw new KeyNotFoundException(nameof(r.SkillEffectId));
                }
                
                return skillEffectRow.skillCategory == SkillCategory.Area;
            });
            var area = new Nekoyume.Game.AreaAttack(skillRow, caster.atk, 1m);
            var result = area.Use(caster);
            var target = caster.targets.First();

            Assert.AreEqual(target.currentHP, target.hp - result.skillInfos.Sum(i => i.Effect));
            Assert.AreEqual(skillEffectRow.hitCount, result.skillInfos.Count());
            foreach (var info in result.skillInfos)
            {
                Assert.NotNull(info.Target);
                Assert.AreEqual(SkillCategory.Area, info.skillCategory);
                Assert.AreEqual(ElementalType.Normal, info.Elemental);
            }
        }

        [Test]
        public void Heal()
        {
            var caster = _simulator.Player;
            var skillRow = ATestSetUp.tableSheets.SkillSheet.OrderedList.First(r =>
            {
                if (!Tables.instance.SkillEffect.TryGetValue(r.SkillEffectId, out var skillEffectRow))
                {
                    throw new KeyNotFoundException(nameof(r.SkillEffectId));
                }
                
                return skillEffectRow.skillType == SkillType.Buff;
            });
            var heal = new Nekoyume.Game.Heal(skillRow, caster.atk, 1m);
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
