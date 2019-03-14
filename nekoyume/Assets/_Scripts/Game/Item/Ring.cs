using System;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Ring : Equipment
    {
        public Ring(Data.Table.Item data) : base(data)
        {
        }

        public override void UpdatePlayer(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
