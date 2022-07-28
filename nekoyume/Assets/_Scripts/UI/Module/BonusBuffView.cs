using Nekoyume.TableData.Crystal;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Nekoyume.Game.ScriptableObject;

namespace Nekoyume.UI.Module
{
    public class BonusBuffView : MonoBehaviour
    {
        [SerializeField]
        private GameObject ssRankVFX;

        [SerializeField]
        private GameObject sRankVFX;

        [SerializeField]
        private GameObject aRankVFX;

        [SerializeField]
        private Button button;

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

        public BonusBuffViewDataScriptableObject BonusBuffViewData { get; set; }

        public Sprite CurrentIcon { get; private set; }

        public string CurrentSkillName { get; private set; }

        public BonusBuffGradeData CurrentGradeData { get; private set; }

        private CrystalRandomBuffSheet.Row _row;

        private UnityAction<CrystalRandomBuffSheet.Row> _onClick;

        private void Awake()
        {
            button.onClick.AddListener(OnClickButton);
        }

        private void OnClickButton() => _onClick?.Invoke(_row);

        public bool UpdateSelected(CrystalRandomBuffSheet.Row rowId)
        {
            var isSelected = rowId.Id == _row.Id;
            selected.SetActive(isSelected);
            return isSelected;
        }

        public void SetData(
            CrystalRandomBuffSheet.Row bonusBuffRow,
            UnityAction<CrystalRandomBuffSheet.Row> onClick)
        {
            _row = bonusBuffRow;
            _onClick = onClick;
            var skillSheet = Game.Game.instance.TableSheets.SkillSheet;
            if (!skillSheet.TryGetValue(bonusBuffRow.SkillId, out var skillRow))
            {
                gameObject.SetActive(false);
                return;
            }

            ssRankVFX.SetActive(bonusBuffRow.Rank == CrystalRandomBuffSheet.Row.BuffRank.SS);
            sRankVFX.SetActive(bonusBuffRow.Rank == CrystalRandomBuffSheet.Row.BuffRank.S);
            aRankVFX.SetActive(bonusBuffRow.Rank == CrystalRandomBuffSheet.Row.BuffRank.A);

            CurrentSkillName = skillRow.GetLocalizedName();
            buffNameText.text = CurrentSkillName;
            CurrentIcon = BonusBuffViewData.GetBonusBuffIcon(skillRow.SkillCategory);
            buffIconImage.sprite = CurrentIcon;
            CurrentGradeData = BonusBuffViewData.GetBonusBuffGradeData(bonusBuffRow.Rank);
            gradeIconImage.sprite = CurrentGradeData.IconSprite;
            gradeBgImage.sprite = CurrentGradeData.BgSprite;
            selectGradeBgImage.sprite = CurrentGradeData.BgSprite;
            gameObject.SetActive(true);
        }
    }
}
