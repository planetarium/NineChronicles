using System;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public class StatBuff : Buff
    {
        public StatBuffSheet.Row RowData { get; }

        protected StatBuff(StatBuffSheet.Row row) : base(row.TargetType, row.Duration)
        {
            RowData = row;
        }

        protected StatBuff(StatBuff value) : base(value)
        {
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

        public override object Clone()
        {
            return new StatBuff(this);
        }
    }
}
