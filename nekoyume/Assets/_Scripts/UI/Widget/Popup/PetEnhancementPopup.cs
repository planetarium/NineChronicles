using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Assets;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData.Pet;
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
    public class PetEnhancementPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private SkeletonGraphic petSkeletonGraphic;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private PetInfoView petInfoView;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private Button submitButton;

        [SerializeField]
        private GameObject buttonDisableObject;

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
        private TextMeshProUGUI ncgCostText;

        [SerializeField]
        private GameObject ncgCostObject;

        [SerializeField]
        private GameObject ncgNotEnoughObject;

        [SerializeField]
        private GameObject costObject;

        [SerializeField]
        private Button plusButton;

        [SerializeField]
        private Button minusButton;

        [SerializeField]
        private Button soulStoneIconButton;

        private readonly List<IDisposable> _disposables = new();
        private readonly ReactiveProperty<int> _enhancementCount = new();

        private const string LevelUpText = "LEVEL UP";
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
            LoadingHelper.PetEnhancement.Subscribe(id =>
            {
                buttonDisableObject.SetActive(_notEnoughBalance || id != 0);
            }).AddTo(gameObject);
            _enhancementCount.Subscribe(value =>
            {
                slider.ForceMove(value);
            }).AddTo(gameObject);
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
            base.Show();
            _petRow = petRow;
            levelUpUIList.ForEach(obj => obj.SetActive(false));
            costObject.SetActive(true);
            titleText.text = SummonText;
            petInfoView.Set(
                L10nManager.Localize($"PET_NAME_{_petRow.Id}"),
                _petRow.Grade
            );
            SetObjectByTargetLevel(_petRow.Id, 0, 1);
            petSkeletonGraphic.skeletonDataAsset = PetRenderingHelper.GetPetSkeletonData(_petRow.Id);
            petSkeletonGraphic.Initialize(true);
            requiredSoulStoneImage.overrideSprite =
                PetRenderingHelper.GetSoulStoneSprite(_petRow.Id);
            submitButton.OnClickAsObservable().Subscribe(_ =>
            {
                if (LoadingHelper.PetEnhancement.Value == 0 && !_notEnoughBalance)
                {
                    Action(_petRow.Id, 1);
                }
                else
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_CAN_NOT_ENTER_PET_MENU"),
                        NotificationCell.NotificationType.Information);
                }
            }).AddTo(_disposables);
        }

        public void ShowForLevelUp(PetState petState)
        {
            base.Show();
            _petRow = TableSheets.Instance.PetSheet[petState.PetId];
            levelUpUIList.ForEach(obj => obj.SetActive(true));
            costObject.SetActive(true);
            titleText.text = LevelUpText;
            var option = TableSheets.Instance.PetOptionSheet[petState.PetId].LevelOptionMap[petState.Level];
            contentText.text = L10nManager.Localize($"PET_DESCRIPTION_{option.OptionType}",
                option.OptionValue);
            petInfoView.Set(L10nManager.Localize($"PET_NAME_{petState.PetId}"),
                _petRow.Grade
            );
            petSkeletonGraphic.skeletonDataAsset =
                PetRenderingHelper.GetPetSkeletonData(petState.PetId);
            petSkeletonGraphic.Initialize(true);
            requiredSoulStoneImage.overrideSprite =
                PetRenderingHelper.GetSoulStoneSprite(petState.PetId);
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
                submitButton.OnClickAsObservable().Subscribe(_ =>
                {
                    if (LoadingHelper.PetEnhancement.Value == 0 && !_notEnoughBalance)
                    {
                        Action(petState.PetId, _targetLevel);
                    }
                    else
                    {
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("UI_CAN_NOT_ENTER_PET_MENU"),
                            NotificationCell.NotificationType.Information);
                    }
                }).AddTo(_disposables);
            }
            else
            {
                levelUpUIList.ForEach(obj => obj.SetActive(false));
                costObject.SetActive(false);
                submitButton.OnClickAsObservable().Subscribe(_ =>
                {
                    Close();
                }).AddTo(_disposables);
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposables.DisposeAllAndClear();
            base.Close(ignoreCloseAnimation);
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

            ncgCostText.text = ncg.ToString();
            soulStoneCostText.text = soulStone.ToString();
            ncgCostObject.SetActive(ncg > 0);
            var ncgCost = States.Instance.GoldBalanceState.Gold.Currency * ncg;
            var soulStoneCost =
                Currency.Legacy(_petRow.SoulStoneTicker, 0, null) *
                soulStone;
            var notEnoughNcg
                = States.Instance.GoldBalanceState.Gold < ncgCost;
            var notEnoughSoulStone
                = States.Instance.AvatarBalance[soulStoneCost.Currency.Ticker] < soulStoneCost;
            _notEnoughBalance = notEnoughNcg || notEnoughSoulStone;
            soulStoneNotEnoughObject.SetActive(notEnoughSoulStone);
            ncgNotEnoughObject.SetActive(notEnoughNcg);
            buttonDisableObject.SetActive(_notEnoughBalance || LoadingHelper.PetEnhancement.Value != 0);
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
                PetRenderingHelper.GetSoulStoneSprite(row.Id),
                soulStoneName,
                count,
                content,
                buttonText);
        }
    }
}
