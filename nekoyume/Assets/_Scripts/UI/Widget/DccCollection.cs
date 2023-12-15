using System;
using System.Collections.Generic;
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
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class DccCollection : Widget
    {
        [SerializeField]
        private Button backButton;

        [SerializeField]
        private Button dccButton;

        [SerializeField]
        private PetSlotScroll scroll;

        [SerializeField]
        private SkeletonGraphic petSkeletonGraphic;

        [SerializeField]
        private PetInfoView infoView;

        [SerializeField]
        private TextMeshProUGUI descriptionText;

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

        [SerializeField]
        private CategoryTabButton allButton;

        [SerializeField]
        private CategoryTabButton petButton;

        private PetSlotViewModel _selectedViewModel;
        private readonly List<IDisposable> _disposables = new();

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = () =>
            {
                Close(true);

#if UNITY_ANDROID || UNITY_IOS
                Game.Event.OnRoomEnter.Invoke(true);
#else
                Find<DccMain>().Show(true);
#endif
            };
            backButton.onClick.AddListener(() => CloseWidget.Invoke());
            dccButton.onClick.AddListener(() =>
            {
                Find<ConfirmConnectPopup>().ShowConnectDcc(true);
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
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _disposables.DisposeAllAndClear();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            UpdateView();
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Mileage);
            allButton.OnClick.Subscribe(tabButton =>
            {
                if (!tabButton.IsToggledOn)
                {
                    tabButton.SetToggledOn();
                    petButton.SetToggledOff();
                }
            }).AddTo(_disposables);
            petButton.OnClick.Subscribe(tabButton =>
            {
                if (!tabButton.IsToggledOn)
                {
                    tabButton.SetToggledOn();
                    allButton.SetToggledOff();
                }
            }).AddTo(_disposables);
            scroll.OnClick.Subscribe(OnClickPetSlot).AddTo(_disposables);
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
                _selectedViewModel.Selected.SetValueAndForceNotify(true);

                var petId = row.Id;
                petSkeletonGraphic.skeletonDataAsset = PetFrontHelper.GetPetSkeletonData(petId);
                petSkeletonGraphic.Initialize(true);
                infoView.Set(petId, row.Grade);

                var petOptionMap = TableSheets.Instance.PetOptionSheet[petId].LevelOptionMap;
                if (States.Instance.PetStates.TryGetPetState(petId, out var petState))
                {
                    var currentLevel = petState.Level;
                    var targetLevel = currentLevel + 1;

                    levelText.text = $"Lv.{petState.Level}";
                    levelUpNotification.SetActive(viewModel.HasNotification.Value);

                    if (TableSheets.Instance.PetCostSheet[petId].TryGetCost(targetLevel, out _))
                    {
                        levelText.color = Color.white;
                        levelUpButtonText.text = L10nManager.Localize("UI_LEVEL_UP");

                        var currentOption = petOptionMap[currentLevel];
                        var targetOption = petOptionMap[targetLevel];
                        descriptionText.text = PetFrontHelper.GetComparisonDescriptionText(
                            currentOption, targetOption);
                    }
                    else
                    {
                        levelText.color = PetFrontHelper.GetUIColor(PetFrontHelper.MaxLevelText);
                        levelUpButtonText.text = L10nManager.Localize("UI_INFO");

                        var currentOption = petOptionMap[currentLevel];
                        descriptionText.text = PetFrontHelper.GetDefaultDescriptionText(
                            currentOption, States.Instance.GameConfigState);
                    }
                }
                else
                {
                    levelText.text = "-";
                    levelText.color = Color.white;
                    levelUpButtonText.text = L10nManager.Localize("UI_SUMMON");
                    levelUpNotification.SetActive(false);

                    var targetOption = petOptionMap[1];
                    descriptionText.text = PetFrontHelper.GetDefaultDescriptionText(
                        targetOption, States.Instance.GameConfigState);
                }
            }
        }
    }
}
