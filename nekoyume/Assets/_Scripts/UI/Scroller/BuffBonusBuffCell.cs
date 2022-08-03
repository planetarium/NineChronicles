using Nekoyume.TableData.Crystal;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class BuffBonusBuffCell : MonoBehaviour
    {
        [SerializeField]
        private BonusBuffViewDataScriptableObject bonusBuffViewData;

        [SerializeField]
        private Image gradeIconImage;

        [SerializeField]
        private Image buffIconImage;

        [SerializeField]
        private TextMeshProUGUI buffNameText;

        public void Set(CrystalRandomBuffSheet.Row itemData)
        {
            var skillSheet = Game.Game.instance.TableSheets.SkillSheet;
            if (!skillSheet.TryGetValue(itemData.SkillId, out var skillRow))
            {
                return;
            }

            var gradeData = bonusBuffViewData.GetBonusBuffGradeData(itemData.Rank);
            gradeIconImage.sprite = gradeData.BgSprite;
            buffIconImage.sprite = bonusBuffViewData.GetBonusBuffIcon(skillRow.SkillCategory);
            buffNameText.text = skillRow.GetLocalizedName();
            buffNameText.color = LocalizationExtension.GetBuffGradeColor(itemData.Rank);
        }
    }
}
