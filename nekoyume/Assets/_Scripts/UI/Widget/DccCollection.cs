using System.Linq;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Pet;
using Nekoyume.UI.Scroller;
using Spine.Unity;
using TMPro;
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
        private PetInfoView infoView;

        [SerializeField]
        private Button lockedButton;

        [SerializeField]
        private Button levelUpButton;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private TextMeshProUGUI levelUpButtonText;

        [SerializeField]
        private GameObject levelUpNotification;

        private PetSlotViewModel _selectedViewModel;

        private const string LevelUpText = "Levelup";
        private const string SummonText = "Summon";

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
            levelUpButton.onClick.AddListener(() =>
            {
                var row = _selectedViewModel.PetRow;
                if (States.Instance.PetStates.TryGetPetState(row.Id, out var petState))
                {
                    Find<PetEnhancementPopup>().ShowForLevelUp(petState);
                }
                else
                {
                    Find<PetEnhancementPopup>().ShowForSummon(row);
                }
            });
            scroll.OnClick.Subscribe(OnClickPetSlot).AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            UpdateView();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Mileage);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<DccMain>().Show(true);
            base.Close(ignoreCloseAnimation);
        }

        public void UpdateView()
        {
            scroll.UpdateData(TableSheets.Instance.PetSheet.Values
                .Select(row =>
                    new PetSlotViewModel(row, PetFrontHelper.HasNotification(row.Id))));
            if (scroll.TryGetFirstItem(out var cell))
            {
                OnClickPetSlot(cell);
            }
        }

        public void OnClickPetSlot(PetSlotViewModel viewModel)
        {
            var row = viewModel.PetRow;
            if (row is not null && !viewModel.Equals(_selectedViewModel))
            {
                _selectedViewModel?.Selected.SetValueAndForceNotify(false);
                _selectedViewModel = viewModel;
                petSkeletonGraphic.skeletonDataAsset =
                    PetFrontHelper.GetPetSkeletonData(row.Id);
                petSkeletonGraphic.Initialize(true);
                _selectedViewModel.Selected.SetValueAndForceNotify(true);
                infoView.Set(row.Id, row.Grade);

                var isOwn = States.Instance.PetStates.TryGetPetState(row.Id, out var petState);
                var costSheet = TableSheets.Instance.PetCostSheet[row.Id];
                var isMaxLevel = !costSheet.TryGetCost((isOwn ? petState.Level : 0) + 1, out _);
                if (isOwn)
                {
                    levelText.text = $"Lv.{petState.Level}";
                    levelText.color = isMaxLevel
                        ? PetFrontHelper.GetUIColor(PetFrontHelper.MaxLevelText)
                        : Color.white;
                    levelUpButtonText.text = isMaxLevel
                        ? "Info"
                        : LevelUpText;
                    levelUpNotification.SetActive(viewModel.HasNotification.Value);
                }
                else
                {
                    levelText.text = "-";
                    levelUpButtonText.text = SummonText;
                    levelUpNotification.SetActive(false);
                }
            }
        }
    }
}
