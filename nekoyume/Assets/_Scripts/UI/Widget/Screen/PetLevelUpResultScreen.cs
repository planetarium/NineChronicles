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
            petInfoView.Set(L10nManager.Localize($"PET_NAME_{petRow.Id}"), petRow.Grade);
            contentText.text = L10nManager.Localize(
                $"PET_DESCRIPTION_TWO_OPTION_{targetOption.OptionType}",
                currentOption.OptionValue,
                targetOption.OptionValue);
            prevLevelText.text = $"<size=30>Lv.</size>{prevPetState.Level}";
            newLevelText.text = $"{action.TargetLevel}";
            petSkeletonGraphic.skeletonDataAsset = PetRenderingHelper.GetPetSkeletonData(petRow.Id);
            petSkeletonGraphic.Initialize(true);
        }
    }
}
