using System;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Shoes : Equipment
    {
        public Shoes(Data.Table.Item data) : base(data)
        {
        }

        public override void UpdatePlayer(Player player)
        {
        }
    }
}
