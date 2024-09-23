using System.Linq;
using Coffee.UIEffects;
using Libplanet.Types.Assets;
using Nekoyume.Helper;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BuyFungibleAssetInformationPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private Scrollbar scrollbar;

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private TextMeshProUGUI countText;

        [SerializeField]
        private Image fungibleAssetImage;

        [SerializeField]
        private Image gradeImage;

        [SerializeField]
        private UIHsvModifier gradeHsv;

        [SerializeField]
        private ItemViewDataScriptableObject itemViewDataScriptableObject;

        private System.Action _onClose;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close());
            CloseWidget = () => Close();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _onClose?.Invoke();
            base.Close(ignoreCloseAnimation);
        }

        public void Show(FungibleAssetValue fav, System.Action onClose)
        {
            _onClose = onClose;
            var grade = 1;
            var id = 0;
            var ticker = fav.Currency.Ticker;
            if (RuneFrontHelper.TryGetRuneData(ticker, out var runeData))
            {
                var sheet = Game.Game.instance.TableSheets.RuneListSheet;
                if (sheet.TryGetValue(runeData.id, out var row))
                {
                    grade = row.Grade;
                    id = runeData.id;
                }
            }

            var petSheet = Game.Game.instance.TableSheets.PetSheet;
            var petRow = petSheet.Values.FirstOrDefault(x => x.SoulStoneTicker == ticker);
            if (petRow is not null)
            {
                grade = petRow.Grade;
                id = petRow.Id;
            }

            fungibleAssetImage.sprite = fav.GetIconSprite();
            nameText.text = fav.GetLocalizedName();
            contentText.text = L10nManager.LocalizeItemDescription(id);
            countText.text = fav.GetQuantityString();
            UpdateGrade(grade);
            scrollbar.value = 1f;
            base.Show();
        }

        private void UpdateGrade(int grade)
        {
            var data = itemViewDataScriptableObject.GetItemViewData(grade);
            gradeImage.overrideSprite = data.GradeBackground;
            gradeHsv.range = data.GradeHsvRange;
            gradeHsv.hue = data.GradeHsvHue;
            gradeHsv.saturation = data.GradeHsvSaturation;
            gradeHsv.value = data.GradeHsvValue;

            var color = LocalizationExtensions.GetItemGradeColor(grade);
            nameText.color = color;
        }
    }
}
