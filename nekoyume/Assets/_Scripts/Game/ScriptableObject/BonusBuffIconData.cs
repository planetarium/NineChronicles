using Nekoyume.Model.Skill;
using System;
using Nekoyume.Editor;
using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [Serializable]
    public class BonusBuffIconData
    {
        [SerializeField]
        [EnumToString(typeof(SkillCategory))]
        private string skillCategory;

        [SerializeField]
        private Sprite iconSprite;

        public SkillCategory SkillCategory()
        {
            Enum.TryParse(typeof(SkillCategory), skillCategory, true, out var skillCategoryEnum);
            return (SkillCategory)skillCategoryEnum;
        }

        public Sprite IconSprite => iconSprite;
    }
}
