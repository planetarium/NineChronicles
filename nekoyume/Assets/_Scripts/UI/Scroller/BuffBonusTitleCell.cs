using Nekoyume.TableData.Crystal;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class BuffBonusTitleCell : MonoBehaviour
    {
        [SerializeField]
        private BonusBuffViewDataScriptableObject bonusBuffViewData;

        [SerializeField]
        private Image gradeIcon;

        public void Set(CrystalRandomBuffSheet.Row.BuffRank rank)
        {
            var gradeData = bonusBuffViewData.GetBonusBuffGradeData(rank);
            gradeIcon.sprite = gradeData.IconSprite;
        }
    }
}
