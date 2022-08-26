using System;
using System.Collections.Generic;
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
            var damage = (int) decimal.Round(Power * RowData.ATKDamageRatio);
            affectedCharacter.GetDamage(damage, false);

            var damageInfos = new List<BattleStatus.Skill.SkillInfo>
            {
                new BattleStatus.Skill.SkillInfo((CharacterBase)affectedCharacter.Clone(), Power, false,
                        SkillCategory.Debuff, simulatorWaveTurn, RowData.ElementalType,
                        RowData.TargetType)
            };

            return new Model.BattleStatus.TickDamage(
                RowData.Id,
                clone,
                damageInfos,
                null);
        }
    }
}
