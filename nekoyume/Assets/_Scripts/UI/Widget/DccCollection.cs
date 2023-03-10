using System.Linq;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using Spine.Unity;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class DccCollection : Widget
    {
        [SerializeField]
        private Button backButton;

        [SerializeField]
        private PetSlotScroll scroll;

        [SerializeField]
        private SkeletonGraphic petSkeletonGraphic;

        [SerializeField]
        private Button lockedButton;

        protected override void Awake()
        {
            base.Awake();
            backButton.onClick.AddListener(() =>
            {
                Close(true);
            });
            lockedButton.onClick.AddListener(() =>
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_INFO_NEW_MERCHANDISE_ADDED_SOON"),
                    NotificationCell.NotificationType.Information);
            });
            scroll.OnClick.Subscribe(viewModel =>
            {
                var row = viewModel.PetRow;
                if (row is not null)
                {
                    petSkeletonGraphic.skeletonDataAsset =
                        PetFrontHelper.GetPetSkeletonData(row.Id);
                    petSkeletonGraphic.Initialize(true);
                    if (States.Instance.PetStates.TryGetPetState(
                            row.Id,
                            out var petState))
                    {
                        Find<PetEnhancementPopup>().ShowForLevelUp(petState);
                    }
                    else
                    {
                        Find<PetEnhancementPopup>().ShowForSummon(viewModel.PetRow);
                    }
                }
            }).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            UpdateView();
        }

        public void UpdateView()
        {
            scroll.UpdateData(TableSheets.Instance.PetSheet.Values
                .Select(row =>
                    new PetSlotViewModel(row, PetFrontHelper.HasNotification(row.Id))));
        }
    }
}
