using System;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Food : ItemUsable
    {
        public Food(Data.Table.Item data, SkillBase skillBase = null, string id = null)
            : base(data, skillBase, id)
        {
        }
    }
}
