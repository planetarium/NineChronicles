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
using BlowAttack = Nekoyume.Game.BlowAttack;
using NormalAttack = Nekoyume.Game.NormalAttack;

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
            caster.Targets.Add(target);
            target.Stats.SetStatForTest(StatType.DEF, 0);
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
            var target = caster.Targets.First();
            var info = result.SkillInfos.First();
            Assert.AreEqual(target.CurrentHP, target.HP - info.Effect);
            Assert.AreEqual(1, result.SkillInfos.Count());
            Assert.NotNull(info.Target);
            Assert.AreEqual(SkillCategory.Normal, info.SkillCategory);
            Assert.AreEqual(ElementalType.Normal, info.ElementalType);
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
            var blow = new BlowAttack(skillRow, caster.ATK, 1m);
            var result = blow.Use(caster);
            var target = caster.Targets.First();
            var info = result.SkillInfos.First();
            var atk = caster.ATK + blow.power;
            if (info.Critical)
                atk = (int) (atk * CharacterBase.CriticalMultiplier);
            Assert.AreEqual(atk, info.Effect);
            Assert.AreEqual(target.CurrentHP, target.HP - info.Effect);
            Assert.AreEqual(1, result.SkillInfos.Count());
            Assert.NotNull(info.Target);
            Assert.AreEqual(SkillCategory.Blow, info.SkillCategory);
            Assert.AreEqual(ElementalType.Normal, info.ElementalType);
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
            var doubleAttack = new Nekoyume.Game.DoubleAttack(skillRow, caster.ATK, 1m);
            var result = doubleAttack.Use(caster);
            var target = caster.Targets.First();

            Assert.AreEqual(target.CurrentHP, target.HP - result.SkillInfos.Sum(i => i.Effect));
            Assert.AreEqual(2, result.SkillInfos.Count());
            foreach (var info in result.SkillInfos)
            {
                Assert.NotNull(info.Target);
                Assert.AreEqual(SkillCategory.Double, info.SkillCategory);
                Assert.AreEqual(ElementalType.Normal, info.ElementalType);
            }
        }

        [Test]
        public void AreaAttack()
        {
            var caster = _simulator.Player;
            var target = caster.Targets.First();
            var lastHPOfTarget = target.HP;
            SkillEffect skillEffectRow = null;
            var skillRow = ATestSetUp.tableSheets.SkillSheet.OrderedList.First(r =>
            {
                if (!Tables.instance.SkillEffect.TryGetValue(r.SkillEffectId, out skillEffectRow))
                {
                    throw new KeyNotFoundException(nameof(r.SkillEffectId));
                }
                
                return skillEffectRow.skillCategory == SkillCategory.Area;
            });
            var area = new Nekoyume.Game.AreaAttack(skillRow, caster.ATK, 1m);
            var result = area.Use(caster);

            Assert.AreEqual(target.CurrentHP, lastHPOfTarget - result.SkillInfos.Sum(i => i.Effect));
            Assert.AreEqual(skillEffectRow.hitCount, result.SkillInfos.Count());
            foreach (var info in result.SkillInfos)
            {
                Assert.NotNull(info.Target);
                Assert.AreEqual(SkillCategory.Area, info.SkillCategory);
                Assert.AreEqual(ElementalType.Normal, info.ElementalType);
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
            var heal = new Nekoyume.Game.HealSkill(skillRow, caster.ATK, 1m);
            caster.CurrentHP -= caster.ATK;
            var result = heal.Use(caster);

            Assert.AreEqual(caster.CurrentHP, caster.HP);
            Assert.AreEqual(1, result.SkillInfos.Count());
            var info = result.SkillInfos.First();
            Assert.AreEqual(caster.ATK, info.Effect);
            Assert.NotNull(info.Target);
            Assert.AreEqual(1, result.SkillInfos.Count());
            Assert.AreEqual(SkillCategory.Normal, info.SkillCategory);
            Assert.Null(info.ElementalType);
        }
    }
}
