using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class Buff : ICloneable
    {
        public int originalDuration;
        public int remainedDuration;

        public BuffSheet.Row RowData { get; }

        protected Buff(BuffSheet.Row row)
        {
            originalDuration = remainedDuration = row.Duration;
            RowData = row;
        }

        protected Buff(Buff value)
        {
            originalDuration = value.RowData.Duration;
            remainedDuration = value.remainedDuration;
            RowData = value.RowData;
        }

        public int Use(CharacterBase characterBase)
        {
            var value = 0;
            switch (RowData.StatModifier.StatType)
            {
                case StatType.HP:
                    value = characterBase.HP;
                    break;
                case StatType.ATK:
                    value = characterBase.ATK;
                    break;
                case StatType.DEF:
                    value = characterBase.DEF;
                    break;
                case StatType.CRI:
                    value = characterBase.CRI;
                    break;
                case StatType.HIT:
                    value = characterBase.HIT;
                    break;
                case StatType.SPD:
                    value = characterBase.SPD;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return RowData.StatModifier.GetModifiedAll(value);
        }

        public IEnumerable<CharacterBase> GetTarget(CharacterBase caster)
        {
            return RowData.TargetType.GetTarget(caster);
        }

        public object Clone()
        {
            return new Buff(this);
        }
    }
}
