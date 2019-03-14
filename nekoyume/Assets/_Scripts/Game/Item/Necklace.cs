using System;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Necklace : Equipment
    {
        public Necklace(Data.Table.Item data) : base(data)
        {
        }

        public override void UpdatePlayer(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
