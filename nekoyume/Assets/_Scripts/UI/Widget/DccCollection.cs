using System.Linq;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Module.Pet;
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

        private PetSlotViewModel _selectedViewModel;

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
            scroll.OnClick.Subscribe(OnClickPetSlot).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            UpdateView();
            if (scroll.TryGetFirstItem(out var cell))
            {
                cell.Selected.SetValueAndForceNotify(true);
                OnClickPetSlot(cell);
                _selectedViewModel = cell;
            }
        }

        public void UpdateView()
        {
            scroll.UpdateData(TableSheets.Instance.PetSheet.Values
                .Select(row =>
                    new PetSlotViewModel(row, PetRenderingHelper.HasNotification(row.Id))));
        }

        public void OnClickPetSlot(PetSlotViewModel viewModel)
        {
            var row = viewModel.PetRow;
            if (row is not null && !viewModel.Equals(_selectedViewModel))
            {
                _selectedViewModel?.Selected.SetValueAndForceNotify(false);
                _selectedViewModel = viewModel;
                petSkeletonGraphic.skeletonDataAsset =
                    PetRenderingHelper.GetPetSkeletonData(row.Id);
                petSkeletonGraphic.Initialize(true);
                _selectedViewModel.Selected.SetValueAndForceNotify(true);
            }
        }
    }
}
