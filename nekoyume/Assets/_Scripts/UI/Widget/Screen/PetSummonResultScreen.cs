using BrunoMikoski.AnimationSequencer;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.State;
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

        [SerializeField]
        private AnimationSequencerController sequencer;

        public void Show(int petId)
        {
            base.Show();
            var petRow = TableSheets.Instance.PetSheet[petId];
            var option = TableSheets.Instance.PetOptionSheet[petId].LevelOptionMap[1];
            petInfoView.Set(petRow.Id, petRow.Grade);
            contentText.text = PetFrontHelper.GetDefaultDescriptionText(
                option, States.Instance.GameConfigState);
            petSkeletonGraphic.skeletonDataAsset = PetFrontHelper.GetPetSkeletonData(petId);
            petSkeletonGraphic.Initialize(true);
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            sequencer.Play();
        }
    }
}
