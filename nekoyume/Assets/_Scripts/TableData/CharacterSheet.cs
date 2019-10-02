using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;
using Nekoyume.Model;

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
            public string Name { get; private set; }
            public SizeType Size { get; private set; }
            public ElementalType Elemental { get; private set; }
            public int HP { get; private set; }
            public int ATK { get; private set; }
            public int DEF { get; private set; }
            public decimal CRI { get; private set; }
            public int LvHP { get; private set; }
            public int LvATK { get; private set; }
            public int LvDEF { get; private set; }
            public decimal LvCRI { get; private set; }
            public float RNG { get; private set; }
            public float RunSpeed { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                Name = fields[1];
                Size = (SizeType) Enum.Parse(typeof(SizeType), fields[2]);
                Elemental = Enum.TryParse<ElementalType>(fields[3], out var elementalType)
                    ? elementalType
                    : ElementalType.Normal;
                HP = int.TryParse(fields[4], out var hp) ? hp : 0;
                ATK = int.TryParse(fields[5], out var damage) ? damage : 0;
                DEF = int.TryParse(fields[6], out var defense) ? defense : 0;
                CRI = decimal.TryParse(fields[7], out var luck) ? luck : 0m;
                LvHP = int.TryParse(fields[8], out var lvHP) ? lvHP : 0;
                LvATK = int.TryParse(fields[9], out var lvDamage) ? lvDamage : 0;
                LvDEF = int.TryParse(fields[10], out var lvDefense) ? lvDefense : 0;
                LvCRI = decimal.TryParse(fields[11], out var lvLuck) ? lvLuck : 0.1m;
                RNG = int.TryParse(fields[12], out var attackRange) ? attackRange : 1f;
                RunSpeed = int.TryParse(fields[13], out var runSpeed) ? runSpeed : 1f;
            }
        }
    }

    public static class CharacterSheetExtension
    {
        public static Stats ToStats(this CharacterSheet.Row row, int level)
        {
            var hp = row.HP;
            var atk = row.ATK;
            var def = row.DEF;
            var cri = row.CRI;
            if (level > 1)
            {
                var multiplier = level - 1;
                hp += row.LvHP * multiplier;
                atk += row.LvATK * multiplier;
                def += row.LvDEF * multiplier;
                cri += row.LvCRI * multiplier;
            }

            var stats = new Stats();
            stats.AddStatValue(StatType.HP, hp);
            stats.AddStatValue(StatType.ATK, atk);
            stats.AddStatValue(StatType.DEF, def);
            stats.AddStatValue(StatType.CRI, cri);

            return stats;
        }
    }
}
