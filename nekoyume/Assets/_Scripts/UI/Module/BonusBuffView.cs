using Nekoyume.TableData.Crystal;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class BonusBuffView : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private BonusBuffViewDataScriptableObject bonusBuffViewData;

        [SerializeField]
        private GameObject selected;

        [SerializeField]
        private TextMeshProUGUI buffNameText;

        [SerializeField]
        private Image buffIconImage;

        [SerializeField]
        private Image gradeIconImage;

        [SerializeField]
        private Image gradeBgImage;

        [SerializeField]
        private Image selectGradeBgImage;

        private CrystalRandomBuffSheet.Row _row;

        public readonly Subject<CrystalRandomBuffSheet.Row> OnSelectedSubject
            = new Subject<CrystalRandomBuffSheet.Row>();

        private void Awake()
        {
            button.onClick.AddListener(() => OnSelectedSubject.OnNext(_row));
        }

        public void UpdateSelected(CrystalRandomBuffSheet.Row bonusBuffRow)
            => selected.SetActive(bonusBuffRow == _row);

        public void SetData(CrystalRandomBuffSheet.Row bonusBuffRow)
        {
            _row = bonusBuffRow;
            var skillSheet = Game.Game.instance.TableSheets.SkillSheet;
            if (!skillSheet.TryGetValue(bonusBuffRow.SkillId, out var skillRow))
            {
                gameObject.SetActive(false);
                return;
            }

            buffNameText.text = skillRow.GetLocalizedName();

            var iconSprite = bonusBuffViewData.GetBonusBuffIcon(skillRow.SkillCategory);
            buffIconImage.sprite = iconSprite;
            var gradeData = bonusBuffViewData.GetBonusBuffGradeData(bonusBuffRow.Rank);
            gradeIconImage.sprite = gradeData.IconSprite;
            gradeBgImage.sprite = gradeData.BgSprite;
            selectGradeBgImage.sprite = gradeData.BgSprite;
            gameObject.SetActive(true);
        }
    }
}
