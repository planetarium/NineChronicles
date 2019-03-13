using System;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Belt : Equipment
    {
        private const int SynergyMultiplier = 2;
        public Belt(Data.Table.Item data) : base(data)
        {
        }
        public override string ToItemInfo()
        {
            return $"체력 +{Data.Param_0}";
        }

        public override void UpdatePlayer(Player player)
        {
            var additionalHP = Data.Param_0;
            if (player.set?.Data.Id == Data.Synergy)
            {
                additionalHP *= SynergyMultiplier;
            }

            player.hp += additionalHP;
        }

    }
}
