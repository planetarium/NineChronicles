using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class PetSummonResultScreen : ScreenWidget
    {
        [SerializeField]
        private TextMeshProUGUI petNameText;

        [SerializeField]
        private TextMeshProUGUI petGradeText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private SkeletonGraphic petSkeletonGraphic;

        public void Show(int petId)
        {
            base.Show();
            var petRow = TableSheets.Instance.PetSheet[petId];
            var option = TableSheets.Instance.PetOptionSheet[petId].LevelOptionMap[1];
            petNameText.text = L10nManager.Localize($"PET_NAME_{petRow.Id}");
            petGradeText.text = L10nManager.Localize($"UI_ITEM_GRADE_{petRow.Grade}");
            contentText.text = L10nManager.Localize($"PET_DESCRIPTION_{option.OptionType}",option.OptionValue);
            petSkeletonGraphic.skeletonDataAsset = PetRenderingHelper.GetPetSkeletonData(petId);
            petSkeletonGraphic.Initialize(true);
        }
    }
}
