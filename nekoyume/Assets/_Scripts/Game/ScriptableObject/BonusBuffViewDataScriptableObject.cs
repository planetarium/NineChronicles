using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Model.Skill;
using Nekoyume.TableData.Crystal;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_BonusBuffViewData", menuName = "Scriptable Object/Bonus Buff View Data",
        order = int.MaxValue)]
    public class BonusBuffViewDataScriptableObject : ScriptableObject
    {
        [field: SerializeField]
        public Sprite FallbackIconSprite { get; set; }

        [field: SerializeField]
        public CrystalRandomBuffSheet.Row.BuffRank FallbackBuffRank { get; set; }

        [SerializeField]
        private List<BonusBuffIconData> bonusBuffIconDatas;

        [SerializeField]
        private List<BonusBuffGradeData> bonusBuffGradeDatas;

        public Sprite GetBonusBuffIcon(SkillCategory skillCategory)
        {
            BonusBuffIconData data = null;
            data = bonusBuffIconDatas.FirstOrDefault(x => x.SkillCategory == skillCategory);
            if (data is null)
            {
                return FallbackIconSprite;
            }

            return data.IconSprite;
        }

        public BonusBuffGradeData GetBonusBuffGradeData(CrystalRandomBuffSheet.Row.BuffRank buffRank)
        {
            BonusBuffGradeData data = null;
            data = bonusBuffGradeDatas.FirstOrDefault(x => x.BuffRank == buffRank);
            if (data is null)
            {
                data = bonusBuffGradeDatas.FirstOrDefault(x => x.BuffRank == FallbackBuffRank);
            }

            return data;
        }
    }
}
