using System.Collections.Generic;
using System.Linq;
using Libplanet;
using Nekoyume;
using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Model;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using NUnit.Framework;
using Tests.PlayMode.Fixtures;
using BlowAttack = Nekoyume.Model.Skill.BlowAttack;
using NormalAttack = Nekoyume.Model.Skill.NormalAttack;

namespace Tests.PlayMode
{
    public class SkillTest : PlayModeTest
    {
        private StageSimulator _stageSimulator;

        [SetUp]
        public void SetUp()
        {
            var random = new Cheat.DebugRandom();
            var address = new Address();
            var agentAddress = new Address();
            var avatarState = new AvatarState(address, agentAddress, 1, Game.instance.TableSheets);

            _stageSimulator = new StageSimulator(
                random,
                avatarState, 
                new List<Consumable>(), 
                1, 
                1,
                Game.instance.TableSheets
            );
            var caster = _stageSimulator.Player;
            var target = (CharacterBase) caster.Clone();
            caster.InitAI();
            caster.Targets.Add(target);
            target.Stats.SetStatForTest(StatType.DEF, 0);
            target.Stats.SetStatForTest(StatType.HIT, 0);
        }

        [TearDown]
        public void TearDown()
        {
            _stageSimulator = null;
        }

        [Test]
        public void NormalAttack()
        {
            var caster = _stageSimulator.Player;
            var attack = caster.Skills.First(s => s is NormalAttack);
            var result = attack.Use(
                caster, 
                0, 
                BuffFactory.GetBuffs(
                    attack,
                    Game.instance.TableSheets.SkillBuffSheet,
                    Game.instance.TableSheets.BuffSheet
                )
            );
            var target = caster.Targets.First();
            var info = result.SkillInfos.First();
            Assert.AreEqual(target.CurrentHP, target.HP - info.Effect);
            Assert.AreEqual(1, result.SkillInfos.Count());
            Assert.NotNull(info.Target);
            Assert.AreEqual(SkillCategory.NormalAttack, info.SkillCategory);
            Assert.AreEqual(ElementalType.Normal, info.ElementalType);
        }

        [Test]
        public void BlowAttack()
        {
            var caster = _stageSimulator.Player;
            var skillRow = Game.instance.TableSheets.SkillSheet.OrderedList.First(r => r.SkillCategory == SkillCategory.BlowAttack);
            var blow = new BlowAttack(skillRow, caster.ATK, 100);
            var result = blow.Use(
                caster, 
                0,
                BuffFactory.GetBuffs(
                    blow,
                    Game.instance.TableSheets.SkillBuffSheet,
                    Game.instance.TableSheets.BuffSheet
                )
            );
            var target = caster.Targets.First();
            var info = result.SkillInfos.First();
            var atk = caster.ATK + blow.power;
            if (info.Critical)
                atk = (int) (atk * CharacterBase.CriticalMultiplier);
            Assert.AreEqual(atk, info.Effect);
            Assert.AreEqual(target.CurrentHP, target.HP - info.Effect);
            Assert.AreEqual(1, result.SkillInfos.Count());
            Assert.NotNull(info.Target);
            Assert.AreEqual(SkillCategory.BlowAttack, info.SkillCategory);
            Assert.AreEqual(ElementalType.Normal, info.ElementalType);
        }

        [Test]
        public void DoubleAttack()
        {
            var caster = _stageSimulator.Player;
            var skillRow = Game.instance.TableSheets.SkillSheet.OrderedList.First(r => r.Id == 100002);
            var doubleAttack = new DoubleAttack(skillRow, caster.ATK, 100);
            var result = doubleAttack.Use(
                caster,
                0,
                BuffFactory.GetBuffs(
                    doubleAttack,
                    Game.instance.TableSheets.SkillBuffSheet,
                    Game.instance.TableSheets.BuffSheet
                )
            );
            var target = caster.Targets.First();

            Assert.AreEqual(target.CurrentHP, target.HP - result.SkillInfos.Sum(i => i.Effect));
            Assert.AreEqual(2, result.SkillInfos.Count());
            foreach (var info in result.SkillInfos)
            {
                Assert.NotNull(info.Target);
                Assert.AreEqual(SkillCategory.DoubleAttack, info.SkillCategory);
                Assert.AreEqual(ElementalType.Normal, info.ElementalType);
            }
        }

        [Test]
        public void AreaAttack()
        {
            var caster = _stageSimulator.Player;
            var target = caster.Targets.First();
            var lastHPOfTarget = target.HP;
            var skillRow = Game.instance.TableSheets.SkillSheet.OrderedList.First(r => r.Id == 100003);
            var area = new AreaAttack(skillRow, caster.ATK, 100);
            var result = area.Use(
                caster,
                0,
                BuffFactory.GetBuffs(
                    area,
                    Game.instance.TableSheets.SkillBuffSheet,
                    Game.instance.TableSheets.BuffSheet
                )
            );

            Assert.AreEqual(target.CurrentHP, lastHPOfTarget - result.SkillInfos.Sum(i => i.Effect));
            Assert.AreEqual(area.skillRow.HitCount, result.SkillInfos.Count());
            foreach (var info in result.SkillInfos)
            {
                Assert.NotNull(info.Target);
                Assert.AreEqual(SkillCategory.AreaAttack, info.SkillCategory);
                Assert.AreEqual(ElementalType.Normal, info.ElementalType);
            }
        }

        [Test]
        public void Heal()
        {
            var caster = _stageSimulator.Player;
            var skillRow = Game.instance.TableSheets.SkillSheet.OrderedList.First(r => r.Id == 200000);
            var heal = new HealSkill(skillRow, caster.ATK, 100);
            caster.CurrentHP -= caster.ATK;
            var result = heal.Use(
                caster,
                0,
                BuffFactory.GetBuffs(
                    heal,
                    Game.instance.TableSheets.SkillBuffSheet,
                    Game.instance.TableSheets.BuffSheet
                )
            );

            Assert.AreEqual(caster.CurrentHP, caster.HP);
            Assert.AreEqual(1, result.SkillInfos.Count());
            var info = result.SkillInfos.First();
            Assert.AreEqual(caster.ATK, info.Effect);
            Assert.NotNull(info.Target);
            Assert.AreEqual(1, result.SkillInfos.Count());
            Assert.AreEqual(SkillCategory.Heal, info.SkillCategory);
            Assert.AreEqual(ElementalType.Normal, info.ElementalType);
        }
    }
}
