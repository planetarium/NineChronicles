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
//        public override string ToItemInfo()
//        {
//            return $"체력 +{Data.param0}";
//        }

        public override void UpdatePlayer(Player player)
        {
//            var additionalHP = Data.param0;
//            if (player.set?.Data.id == Data.Synergy)
//            {
//                additionalHP *= SynergyMultiplier;
//            }
//
//            player.hp += additionalHP;
//            player.hpMax += additionalHP;
        }

    }
}
