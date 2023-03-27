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

        public void Show(PetEnhancement action)
        {
            base.Show();
            var petRow = TableSheets.Instance.PetSheet[action.PetId];
            States.Instance.PetStates.TryGetPetState(petRow.Id, out var prevPetState);
            var currentOption = TableSheets.Instance.PetOptionSheet[action.PetId].LevelOptionMap[prevPetState.Level];
            var targetOption = TableSheets.Instance.PetOptionSheet[action.PetId].LevelOptionMap[action.TargetLevel];
            petInfoView.Set(petRow.Id, petRow.Grade);
            contentText.text = PetFrontHelper.GetComparisonDescriptionText(
                currentOption, targetOption);
            prevLevelText.text = $"<size=30>Lv.</size>{prevPetState.Level}";
            newLevelText.text = $"{action.TargetLevel}";
            petSkeletonGraphic.skeletonDataAsset = PetFrontHelper.GetPetSkeletonData(petRow.Id);
            petSkeletonGraphic.Initialize(true);
        }
    }
}
