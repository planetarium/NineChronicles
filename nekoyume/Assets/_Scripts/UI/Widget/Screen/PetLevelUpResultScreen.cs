using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Module.Pet;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class PetLevelUpResultScreen : ScreenWidget
    {
        [SerializeField]
        private PetInfoView petInfoView;

        [SerializeField]
        private TextMeshProUGUI prevLevelText;

        [SerializeField]
        private TextMeshProUGUI newLevelText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private SkeletonGraphic petSkeletonGraphic;

        public void Show(int petId, int prevLevel, int targetLevel)
        {
            base.Show();

            var petRow = TableSheets.Instance.PetSheet[petId];
            var optionMap = TableSheets.Instance.PetOptionSheet[petId].LevelOptionMap;

            petInfoView.Set(petId, petRow.Grade);
            contentText.text = PetFrontHelper.GetComparisonDescriptionText(
                optionMap[prevLevel], optionMap[targetLevel]);
            prevLevelText.text = $"<size=30>Lv.</size>{prevLevel}";
            newLevelText.text = $"{targetLevel}";
            petSkeletonGraphic.skeletonDataAsset = PetFrontHelper.GetPetSkeletonData(petId);
            petSkeletonGraphic.Initialize(true);
        }
    }
}
