using System;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Weapon : Equipment
    {
        private const string SpritePath = "images/equipment/{0}";
        private const string SuffixId = "0000";

        public static Sprite GetSprite(Equipment equipment = null)
        {
            var id = equipment?.Data.resourceId;
            // FIXME 속성무기 스프라이트가 준비되면 아이디를 그대로 사용
            if (id != null)
            {
                var sub = id.ToString().Substring(0, 4);
                id = int.Parse(sub + SuffixId);
            }
            return Resources.Load<Sprite>(string.Format(SpritePath, id));
        }

        public Weapon(Data.Table.Item data, SkillBase skillBase = null)
            : base(data, skillBase)
        {
        }
    }
}
