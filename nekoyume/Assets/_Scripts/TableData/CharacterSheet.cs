using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
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
            public int Resource { get; private set; }
            public string Size { get; private set; }
            public Elemental.ElementalType Elemental { get; private set; }
            public int HP { get; private set; }
            public int Damage { get; private set; }
            public int Defense { get; private set; }
            public decimal Luck { get; private set; }
            public int LvHP { get; private set; }
            public int LvDamage { get; private set; }
            public int LvDefense { get; private set; }
            public decimal LvLuck { get; private set; }
            public float AttackRange { get; private set; }
            public float RunSpeed { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                Name = fields[1];
                Resource = int.TryParse(fields[2], out var resource) ? resource : 0;
                Size = string.IsNullOrEmpty(fields[3]) ? "s" : fields[3];
                Elemental = Enum.TryParse<Elemental.ElementalType>(fields[4], out var elementalType)
                    ? elementalType
                    : Data.Table.Elemental.ElementalType.Normal;
                HP = int.TryParse(fields[5], out var hp) ? hp : 0;
                Damage = int.TryParse(fields[6], out var damage) ? damage : 0;
                Defense = int.TryParse(fields[7], out var defense) ? defense : 0;
                Luck = decimal.TryParse(fields[8], out var luck) ? luck : 0m;
                LvHP = int.TryParse(fields[9], out var lvHP) ? lvHP : 0;
                LvDamage = int.TryParse(fields[10], out var lvDamage) ? lvDamage : 0;
                LvDefense = int.TryParse(fields[11], out var lvDefense) ? lvDefense : 0;
                LvLuck = decimal.TryParse(fields[12], out var lvLuck) ? lvLuck : 0.1m;
                AttackRange = int.TryParse(fields[13], out var attackRange) ? attackRange : 1f;
                RunSpeed = int.TryParse(fields[14], out var runSpeed) ? runSpeed : 1f;
            }
        }

        public CharacterSheet(string csv) : base(csv)
        {
        }
    }

    public static class CharacterSheetExtension
    {
        public static Stats ToStats(this CharacterSheet.Row row, int level)
        {
            var hp = row.HP;
            var damage = row.Damage;
            var defense = row.Defense;
            var luck = row.Luck;
            if (level > 1)
            {
                var multiplier = level - 1;
                hp += row.LvHP * multiplier;
                damage += row.LvDamage * multiplier;
                defense += row.LvDefense * multiplier;
                luck += row.LvLuck * multiplier;
            }

            var stats = new Stats();
            stats.SetStatValue("damage", damage);
            stats.SetStatValue("defense", defense);
            stats.SetStatValue("health", hp);
            stats.SetStatValue("luck", luck);

            return stats;
        }
    }
}
