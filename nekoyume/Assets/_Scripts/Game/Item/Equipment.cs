using System;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Equipment : ItemUsable
    {
        public bool equipped = false;
        private int _level = 0;
        private int _enchantCount = 0;
        private StatsMap[] _stats;

        public Equipment(Data.Table.Item data)
            : base(data)
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
            _stats = new[] {stat1, stat2};
        }

        public override bool Use()
        {
            if (!equipped)
            {
                return Equip();
            }
            else
            {
                return Unequip();
            }
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

        public void UpdatePlayer(Player player)
        {
            foreach (var stat in _stats)
            {
                stat.UpdatePlayer(player);
            }

        }

        public override string ToItemInfo()
        {
            var infos = _stats
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
