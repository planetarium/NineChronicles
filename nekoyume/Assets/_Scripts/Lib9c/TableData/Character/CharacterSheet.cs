using System;
using System.Collections.Generic;
using Nekoyume.Model.Character;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Stat;

namespace Nekoyume.TableData
{
    [Serializable]
    public class CharacterSheet : Sheet<int, CharacterSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public SizeType SizeType { get; private set; }
            public ElementalType ElementalType { get; private set; }
            public decimal HP { get; private set; }
            public decimal ATK { get; private set; }
            public decimal DEF { get; private set; }
            public decimal CRI { get; private set; }
            public decimal HIT { get; private set; }
            public decimal SPD { get; private set; }
            public decimal LvHP { get; private set; }
            public decimal LvATK { get; private set; }
            public decimal LvDEF { get; private set; }
            public decimal LvCRI { get; private set; }
            public decimal LvHIT { get; private set; }
            public decimal LvSPD { get; private set; }
            public float AttackRange { get; private set; }
            public float RunSpeed { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                SizeType = (SizeType) Enum.Parse(typeof(SizeType), fields[1]);
                ElementalType = Enum.TryParse<ElementalType>(fields[2], out var elementalType)
                    ? elementalType
                    : ElementalType.Normal;
                HP = decimal.TryParse(fields[3], out var hp) ? hp : 0m;
                ATK = decimal.TryParse(fields[4], out var damage) ? damage : 0m;
                DEF = decimal.TryParse(fields[5], out var defense) ? defense : 0m;
                CRI = decimal.TryParse(fields[6], out var cri) ? cri : 0m;
                HIT = decimal.TryParse(fields[7], out var hit) ? hit : 0m;
                SPD = decimal.TryParse(fields[8], out var spd) ? spd : 0m;
                LvHP = decimal.TryParse(fields[9], out var lvHP) ? lvHP : 0m;
                LvATK = decimal.TryParse(fields[10], out var lvDamage) ? lvDamage : 0m;
                LvDEF = decimal.TryParse(fields[11], out var lvDefense) ? lvDefense : 0m;
                LvCRI = decimal.TryParse(fields[12], out var lvCri) ? lvCri : 0m;
                LvHIT = decimal.TryParse(fields[13], out var lvHit) ? lvHit : 0m;
                LvSPD = decimal.TryParse(fields[14], out var lvSpd) ? lvSpd : 0m;
                AttackRange = float.TryParse(fields[15], out var attackRange) ? attackRange : 1f;
                RunSpeed = float.TryParse(fields[16], out var runSpeed) ? runSpeed : 1f;
            }
        }
        
        public CharacterSheet() : base(nameof(CharacterSheet))
        {
        }
    }

    public static class CharacterSheetExtension
    {
        public static StatsMap ToStats(this CharacterSheet.Row row, int level)
        {
            var hp = row.HP;
            var atk = row.ATK;
            var def = row.DEF;
            var cri = row.CRI;
            var hit = row.HIT;
            var spd = row.SPD;
            if (level > 1)
            {
                var multiplier = level - 1;
                hp += row.LvHP * multiplier;
                atk += row.LvATK * multiplier;
                def += row.LvDEF * multiplier;
                cri += row.LvCRI * multiplier;
                hit += row.LvHIT * multiplier;
                spd += row.LvSPD * multiplier;
            }

            var stats = new StatsMap();
            stats.AddStatValue(StatType.HP, hp);
            stats.AddStatValue(StatType.ATK, atk);
            stats.AddStatValue(StatType.DEF, def);
            stats.AddStatValue(StatType.CRI, cri);
            stats.AddStatValue(StatType.HIT, hit);
            stats.AddStatValue(StatType.SPD, spd);

            return stats;
        }
    }
}
