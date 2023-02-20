using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class PetLevelUpResultScreen : ScreenWidget
    {
        [SerializeField]
        private TextMeshProUGUI petNameText;

        [SerializeField]
        private TextMeshProUGUI petGradeText;

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
            var option = TableSheets.Instance.PetOptionSheet[action.PetId].LevelOptionMap[action.TargetLevel];
            States.Instance.PetStates.TryGetPetState(petRow.Id, out var prevPetState);
            petNameText.text = L10nManager.Localize($"PET_NAME_{petRow.Id}");
            petGradeText.text = L10nManager.Localize($"UI_ITEM_GRADE_{petRow.Grade}");
            contentText.text = L10nManager.Localize($"PET_DESCRIPTION_{option.OptionType}",option.OptionValue);
            prevLevelText.text = $"<size=30>Lv.</size>{prevPetState.Level}";
            newLevelText.text = $"{action.TargetLevel}";
            petSkeletonGraphic.skeletonDataAsset = PetRenderingHelper.GetPetSkeletonData(petRow.Id);
            petSkeletonGraphic.Initialize(true);
        }
    }
}
