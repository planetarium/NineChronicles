using System;
using System.Linq;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Equipment : ItemUsable
    {
        public bool equipped = false;
        private int _level = 0;
        private int _enchantCount = 0;

        public Equipment(Data.Table.Item data, float skillChance = 0f, SkillEffect skillEffect = null,
            Data.Table.Elemental.ElementalType skillElementalType = Nekoyume.Data.Table.Elemental.ElementalType.Normal)
            : base(data, skillChance, skillEffect, skillElementalType)
        {
            var stat1 = new StatsMap
            {
                Key = Data.ability1,
                Value = Data.value1,
            };
            var stat2 = new StatsMap
            {
                Key = Data.ability2,
                Value = Data.value2,
            };
            //TODO 논의후 테이블에 제대로 설정되야함.
            var stat3 = new StatsMap
            {
                Key = "turnSpeed",
                Value = Data.turnSpeed,
            };
            //TODO 장비대신 스킬별 사거리를 사용해야함.
            var stat4 = new StatsMap
            {
                Key = "attackRange",
                Value = Data.attackRange,
            };
            Stats = new[] {stat1, stat2, stat3, stat4};
        }

        public bool Equip()
        {
            equipped = true;
            return true;
        }

        public bool Unequip()
        {
            equipped = false;
            return true;
        }

        public bool Enchant()
        {
            _enchantCount++;
            return true;
        }

        public bool LevelUp()
        {
            _level++;
            return true;
        }

        public override string ToItemInfo()
        {
            var infos = Stats
                .Select(stat => stat.GetInformation())
                .Where(info => !string.IsNullOrEmpty(info));
            return string.Join(Environment.NewLine, infos);
        }

    }

    [Serializable]
    public class StatsMap : IStatsMap
    {
        public string Key;
        public float Value;

        public void UpdatePlayer(Player player)
        {
            switch (Key)
            {
                case "damage":
                    player.atk += (int)Value;
                    break;
                case "defense":
                    player.def += (int)Value;
                    break;
                case "health":
                    player.currentHP += (int)Value;
                    player.hp += (int)Value;
                    break;
                case "luck":
                    player.luck += Value / 100;
                    break;
                case "turnSpeed":
                    player.TurnSpeed = Value;
                    break;
                case "attackRange":
                    player.attackRange = Value;
                    break;
            }
        }

        public string GetInformation()
        {
            switch (Key)
            {
                case "damage":
                    return $"공격력 +{Value}";
                case "defense":
                    return $"방어력 +{Value}";
                case "health":
                    return $"체력 +{Value}";
                case "luck":
                    return $"행운 +{Value}";
                default:
                    return "";
            }
        }
    }

    public interface IStatsMap
    {
        void UpdatePlayer(Player player);
        string GetInformation();
    }
}
