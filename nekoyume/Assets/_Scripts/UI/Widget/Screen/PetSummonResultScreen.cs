using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.UI.Module.Pet;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class PetSummonResultScreen : ScreenWidget
    {
        [SerializeField]
        private PetInfoView petInfoView;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private SkeletonGraphic petSkeletonGraphic;

        public void Show(int petId)
        {
            base.Show();
            var petRow = TableSheets.Instance.PetSheet[petId];
            var option = TableSheets.Instance.PetOptionSheet[petId].LevelOptionMap[1];
            petInfoView.Set(L10nManager.Localize($"PET_NAME_{petRow.Id}"), petRow.Grade);
            contentText.text = L10nManager.Localize($"PET_DESCRIPTION_{option.OptionType}",option.OptionValue);
            petSkeletonGraphic.skeletonDataAsset = PetFrontHelper.GetPetSkeletonData(petId);
            petSkeletonGraphic.Initialize(true);
        }
    }
}
