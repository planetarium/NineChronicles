using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Helm : Equipment
    {
        public Helm(Data.Table.Item data, Guid id, Skill skill = null)
            : base(data, id, skill)
        {
        }
    }
}
