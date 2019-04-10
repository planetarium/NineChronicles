using System;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class SetEffect : Row
    {
        public int id = 0;
        public int setId = 0;
        public int setCount = 0;
        public string ability = "";
        public float value = 0f;

        public class SetEffectMap : IStatsMap
        {
            public string Key;
            public float Value;

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
                        player.luck += Value;
                        break;
                }
            }
        }
        public SetEffectMap ToSetEffectMap()
        {
            return new SetEffectMap
            {
                Key = ability,
                Value = value,
            };
        }
    }
}
