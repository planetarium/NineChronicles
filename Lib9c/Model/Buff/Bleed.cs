using System;
using System.Collections.Generic;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class Bleed : ActionBuff
    {
        public int Power { get; }

        public Bleed(ActionBuffSheet.Row row, int power) : base(row)
        {
            Power = power;
        }

        public Bleed(SkillCustomField customField, ActionBuffSheet.Row row) : base(customField, row)
        {
            Power = customField.BuffValue;
        }

        protected Bleed(Bleed value) : base(value)
        {
            Power = value.Power;
        }

        public override object Clone()
        {
            return new Bleed(this);
        }

        public override BattleStatus.Skill GiveEffect(
            CharacterBase affectedCharacter,
            int simulatorWaveTurn)
        {
            var clone = (CharacterBase)affectedCharacter.Clone();
            var originalDamage = (int) decimal.Round(Power * RowData.ATKPowerRatio);
            var damage = affectedCharacter.GetDamage(originalDamage, false);
            affectedCharacter.CurrentHP -= damage;

            var damageInfos = new List<BattleStatus.Skill.SkillInfo>
            {
                new BattleStatus.Skill.SkillInfo((CharacterBase)affectedCharacter.Clone(), damage, false,
                        SkillCategory.Debuff, simulatorWaveTurn, RowData.ElementalType,
                        RowData.TargetType)
            };

            return new Model.BattleStatus.TickDamage(
                RowData.Id,
                clone,
                damageInfos,
                null);
        }

        public override ArenaSkill GiveEffectForArena(
            ArenaCharacter affectedCharacter,
            int simulatorWaveTurn)
        {
            var clone = (ArenaCharacter)affectedCharacter.Clone();
            var originalDamage = (int)decimal.Round(Power * RowData.ATKPowerRatio);
            var damage = affectedCharacter.GetDamage(originalDamage, false);
            affectedCharacter.CurrentHP -= damage;

            var damageInfos = new List<ArenaSkill.ArenaSkillInfo>
            {
                new ArenaSkill.ArenaSkillInfo((ArenaCharacter)affectedCharacter.Clone(), damage, false,
                        SkillCategory.Debuff, simulatorWaveTurn, RowData.ElementalType,
                        RowData.TargetType)
            };

            return new ArenaTickDamage(
                clone,
                damageInfos,
                null);
        }
    }
}
