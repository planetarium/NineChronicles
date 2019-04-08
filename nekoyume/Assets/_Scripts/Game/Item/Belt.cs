using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Belt : Equipment
    {
        private const int SynergyMultiplier = 2;
        public Belt(Data.Table.Item data) : base(data)
        {
        }

    }
}
