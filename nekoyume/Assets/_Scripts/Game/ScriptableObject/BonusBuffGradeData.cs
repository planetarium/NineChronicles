using Nekoyume.Model.Skill;
using Nekoyume.TableData.Crystal;
using System;
using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [Serializable]
    public class BonusBuffGradeData
    {
        [SerializeField]
        private CrystalRandomBuffSheet.Row.BuffRank buffRank;

        [SerializeField]
        private Sprite iconSprite;

        [SerializeField]
        private Sprite bgSprite;

        [SerializeField]
        private Sprite smallBgSprite;

        public CrystalRandomBuffSheet.Row.BuffRank BuffRank => buffRank;

        public Sprite IconSprite => iconSprite;

        public Sprite BgSprite => bgSprite;

        public Sprite SmallBgSprite => smallBgSprite;
    }
}
