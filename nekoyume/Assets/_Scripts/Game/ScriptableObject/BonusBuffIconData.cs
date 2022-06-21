using Nekoyume.Model.Skill;
using System;
using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [Serializable]
    public class BonusBuffIconData
    {
        [SerializeField]
        private SkillCategory skillCategory;

        [SerializeField]
        private Sprite iconSprite;

        public SkillCategory SkillCategory => skillCategory;

        public Sprite IconSprite => iconSprite;
    }
}
