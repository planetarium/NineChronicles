using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
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
    using TableData;
    using UniRx;
    public class PetEnhancementPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private SkeletonGraphic petSkeletonGraphic;

        [SerializeField]
        private PetInfoView petInfoView;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private ConditionalCostButton submitButton;

        [SerializeField]
        private List<GameObject> levelUpUIList;

        [SerializeField]
        private TextMeshProUGUI currentLevelText;

        [SerializeField]
        private TextMeshProUGUI targetLevelText;

        [SerializeField]
        private SweepSlider slider;

        [SerializeField]
        private Image requiredSoulStoneImage;

        [SerializeField]
        private TextMeshProUGUI soulStoneCostText;

        [SerializeField]
        private GameObject soulStoneNotEnoughObject;

        [SerializeField]
        private GameObject costObject;

        [SerializeField]
        private Button plusButton;

        [SerializeField]
        private Button minusButton;

        [SerializeField]
        private Button soulStoneIconButton;

        [SerializeField]
        private TextMeshProUGUI maxLevelReachedText;

        private readonly List<IDisposable> _disposables = new();
        private readonly ReactiveProperty<int> _enhancementCount = new();

        private const string LevelUpText = "Levelup";
        private const string SummonText = "Summon";

        private int _targetLevel;
        private int _sliderMax;
        private int _sliderCurrentValue;
        private bool _notEnoughBalance;
        private PetSheet.Row _petRow;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close());
            plusButton.onClick.AddListener(() =>
            {
                _enhancementCount.Value = Math.Min(_sliderCurrentValue + 1, _sliderMax);
            });
            minusButton.onClick.AddListener(() =>
            {
                _enhancementCount.Value = Math.Max(_sliderCurrentValue - 1, 1);
            });
            soulStoneIconButton.onClick.AddListener(() =>
            {
                OnClickSoulStone(_petRow);
            });
        }

        public void ShowForSummon(PetSheet.Row petRow)
        {
            Show();
            _petRow = petRow;
            levelUpUIList.ForEach(obj => obj.SetActive(false));
            maxLevelReachedText.gameObject.SetActive(false);
            costObject.SetActive(true);
            submitButton.gameObject.SetActive(true);
            submitButton.Text = SummonText;
            petInfoView.Set(
                _petRow.Id,
                _petRow.Grade
            );
            SetObjectByTargetLevel(_petRow.Id, 0, 1);
            petSkeletonGraphic.skeletonDataAsset = PetFrontHelper.GetPetSkeletonData(_petRow.Id);
            petSkeletonGraphic.Initialize(true);
            requiredSoulStoneImage.overrideSprite =
                PetFrontHelper.GetSoulStoneSprite(_petRow.Id);
            submitButton.OnClickSubject.Subscribe(state =>
            {
                switch (state)
                {
                    case ConditionalButton.State.Normal:
                        Action(_petRow.Id, 1);
                        break;
                    case ConditionalButton.State.Conditional:
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_NOT_ENOUGH_NCG"),
                            NotificationCell.NotificationType.Information);
                        break;
                    case ConditionalButton.State.Disabled:
                        break;
                }
            }).AddTo(_disposables);
            submitButton.OnClickDisabledSubject.Subscribe(_ =>
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_CAN_NOT_ENTER_PET_MENU"),
                    NotificationCell.NotificationType.Information);
            }).AddTo(_disposables);
        }

        public void ShowForLevelUp(PetState petState)
        {
            Show();
            _petRow = TableSheets.Instance.PetSheet[petState.PetId];
            levelUpUIList.ForEach(obj => obj.SetActive(true));
            costObject.SetActive(true);
            submitButton.gameObject.SetActive(true);
            maxLevelReachedText.gameObject.SetActive(false);
            submitButton.Text = LevelUpText;
            var option = TableSheets.Instance.PetOptionSheet[petState.PetId].LevelOptionMap[petState.Level];
            contentText.text = L10nManager.Localize($"PET_DESCRIPTION_{option.OptionType}",
                option.OptionValue);
            petInfoView.Set(_petRow.Id,
                _petRow.Grade
            );
            petSkeletonGraphic.skeletonDataAsset =
                PetFrontHelper.GetPetSkeletonData(petState.PetId);
            petSkeletonGraphic.Initialize(true);
            requiredSoulStoneImage.overrideSprite =
                PetFrontHelper.GetSoulStoneSprite(petState.PetId);
            currentLevelText.text = $"<size=30>Lv.</size>{petState.Level}";
            var maxLevel = TableSheets.Instance.PetCostSheet[petState.PetId].Cost
                .OrderBy(data => data.Level)
                .Last()
                .Level;
            if (petState.Level < maxLevel)
            {
                SetObjectByTargetLevel(petState.PetId, petState.Level, petState.Level + 1);
                _sliderMax = maxLevel - petState.Level;
                slider.Set(
                    1,
                    _sliderMax,
                    1,
                    _sliderMax,
                    1,
                    value =>
                    {
                        _sliderCurrentValue = value;
                        SetObjectByTargetLevel(petState.PetId, petState.Level, value + petState.Level);
                    });
                submitButton.OnClickSubject.Subscribe(state =>
                {
                    switch (state)
                    {
                        case ConditionalButton.State.Normal:
                            Action(_petRow.Id, _targetLevel);
                            break;
                        case ConditionalButton.State.Conditional:
                            OneLineSystem.Push(
                                MailType.System,
                                L10nManager.Localize("UI_NOT_ENOUGH_NCG"),
                                NotificationCell.NotificationType.Information);
                            break;
                        case ConditionalButton.State.Disabled:
                            break;
                    }
                }).AddTo(_disposables);
                submitButton.OnClickDisabledSubject.Subscribe(_ =>
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_CAN_NOT_ENTER_PET_MENU"),
                        NotificationCell.NotificationType.Information);
                }).AddTo(_disposables);
            }
            else
            {
                levelUpUIList.ForEach(obj => obj.SetActive(false));
                costObject.SetActive(false);
                submitButton.gameObject.SetActive(false);
                maxLevelReachedText.gameObject.SetActive(true);
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            LoadingHelper.PetEnhancement.Subscribe(id =>
            {
                // buttonDisableObject.SetActive(_notEnoughBalance || id != 0);
            }).AddTo(_disposables);
            _enhancementCount.Subscribe(value =>
            {
                slider.ForceMove(value);
            }).AddTo(_disposables);
        }

        private void SetObjectByTargetLevel(int petId, int currentLevel, int targetLevel)
        {
            _targetLevel = targetLevel;
            targetLevelText.text = _targetLevel.ToString();
            var (ncg, soulStone) = PetHelper.CalculateEnhancementCost(
                TableSheets.Instance.PetCostSheet,
                petId,
                currentLevel,
                _targetLevel);
            var targetOption = TableSheets.Instance.PetOptionSheet[petId].LevelOptionMap[_targetLevel];
            if (targetLevel == 1)
            {
                contentText.text =
                    L10nManager.Localize($"PET_DESCRIPTION_{targetOption.OptionType}",
                        targetOption.OptionValue);
            }
            else
            {
                var currentOption = TableSheets.Instance.PetOptionSheet[petId].LevelOptionMap[currentLevel];
                contentText.text =
                    L10nManager.Localize($"PET_DESCRIPTION_TWO_OPTION_{targetOption.OptionType}",
                        currentOption.OptionValue,
                        targetOption.OptionValue);
            }

            soulStoneCostText.text = soulStone.ToString();
            var ncgCost = States.Instance.GoldBalanceState.Gold.Currency * ncg;
            var soulStoneCost =
                PetHelper.GetSoulstoneCurrency(_petRow.SoulStoneTicker) *
                soulStone;
            var enoughNcg
                = States.Instance.GoldBalanceState.Gold >= ncgCost;
            var enoughSoulStone
                = States.Instance.AvatarBalance[soulStoneCost.Currency.Ticker] >= soulStoneCost;
            var enough = enoughNcg && enoughSoulStone;
            _notEnoughBalance = !enough;
            soulStoneNotEnoughObject.SetActive(_notEnoughBalance);
            submitButton.SetCost(CostType.NCG, ncg);
            if (LoadingHelper.PetEnhancement.Value != 0)
            {
                submitButton.Interactable = enough && LoadingHelper.PetEnhancement.Value == 0;
            }
            else
            {
                submitButton.Interactable = true;
                submitButton.UpdateObjects();
            }
        }

        private void Action(int petId, int targetLevel)
        {
            ActionManager.Instance.PetEnhancement(petId, targetLevel).Subscribe();
            LoadingHelper.PetEnhancement.Value = petId;
            Close();
        }

        private void OnClickSoulStone(PetSheet.Row row)
        {
            var popup = Find<MaterialNavigationPopup>();
            var soulStoneName = L10nManager.Localize($"ITEM_NAME_{row.Id}");
            var count = States.Instance.AvatarBalance[row.SoulStoneTicker].GetQuantityString();
            var content = L10nManager.Localize($"ITEM_DESCRIPTION_{row.Id}");
            var buttonText = L10nManager.Localize("UI_GO_TO_MARKET");

            void Callback()
            {
                Find<DccCollection>().Close(true);
                Find<DccMain>().Close(true);
                base.Close(true);
                Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                var shop = Find<ShopBuy>();
                shop.Show();
            }

            popup.Show(
                Callback,
                PetFrontHelper.GetSoulStoneSprite(row.Id),
                soulStoneName,
                count,
                content,
                buttonText);
        }

        protected override void OnDisable()
        {
            _disposables.DisposeAllAndClear();
            base.OnDisable();
        }
    }
}
