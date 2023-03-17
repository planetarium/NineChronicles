using System;
using System.Linq;
using System.Numerics;
using Coffee.UIEffects;
using Lib9c.Model.Order;
using Libplanet.Assets;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using ShopItem = Nekoyume.UI.Model.ShopItem;

namespace Nekoyume.UI
{
    using System.Collections;
    using UniRx;
    using UnityEngine.UI;

    public class FungibleAssetTooltip : NewVerticalTooltipWidget
    {
        protected override PivotPresetType TargetPivotPresetType => PivotPresetType.TopRight;

        [SerializeField]
        private ItemTooltipBuy buy;

        [SerializeField]
        private ItemTooltipSell sell;

        [SerializeField]
        private Button registerButton;

        [SerializeField]
        private Scrollbar scrollbar;

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private TextMeshProUGUI countText;

        [SerializeField]
        private TextMeshProUGUI levelLimitText;

        [SerializeField]
        private TextMeshProUGUI gradeText;

        [SerializeField]
        private TextMeshProUGUI subTypeText;

        [SerializeField]
        private Image spacerImage;

        [SerializeField]
        private Image fungibleAssetImage;

        [SerializeField]
        private Image gradeImage;

        [SerializeField]
        private UIHsvModifier gradeHsv;

        [SerializeField]
        private ItemViewDataScriptableObject itemViewDataScriptableObject;

        private System.Action _onClose;
        private System.Action _onRegister;
        private bool _isPointerOnScrollArea;
        private bool _isClickedButtonArea;

        protected override void Awake()
        {
            base.Awake();
            CloseWidget = () => Close();
            SubmitWidget = () => Close();
            registerButton.onClick.AddListener(() =>
            {
                _onRegister?.Invoke();
                Close(true);
            });
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _onClose?.Invoke();
            _isPointerOnScrollArea = false;
            _isClickedButtonArea = false;
            base.Close(ignoreCloseAnimation);
        }

        public virtual void Show(
            InventoryItem item,
            System.Action onClose)
        {
            registerButton.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            buy.gameObject.SetActive(false);

            UpdateInformation(item.FungibleAssetValue, onClose);
            base.Show();
            StartCoroutine(CoUpdate(panel.gameObject));
        }

        public void Show(
            ShopItem item,
            System.Action onBuy,
            System.Action onClose)
        {
            registerButton.gameObject.SetActive(false);
            buy.gameObject.SetActive(true);
            sell.gameObject.SetActive(false);
            buy.Set(item.FungibleAssetProduct.RegisteredBlockIndex + Order.ExpirationInterval,
                (BigInteger)item.FungibleAssetProduct.Price * States.Instance.GoldBalanceState.Gold.Currency,
                ()=>
                {
                    onBuy?.Invoke();
                    Close();
                });
            UpdateInformation(item.FungibleAssetValue, onClose);
            base.Show();
            StartCoroutine(CoUpdate(buy.gameObject));
        }

        /// <summary>
        /// A function that is displayed when clicking on a product that is already on sale in the Market
        /// </summary>
        public void Show(
            ShopItem item,
            int apStoneCount,
            Action<ConditionalButton.State> onRegister,
            Action<ConditionalButton.State> onSellCancellation,
            System.Action onClose)
        {
            registerButton.gameObject.SetActive(false);
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(true);
            sell.Set(
                item.FungibleAssetProduct.RegisteredBlockIndex + Order.ExpirationInterval,
                apStoneCount,
                state =>
                {
                    onSellCancellation?.Invoke(state);
                    Close();
                }, state =>
                {
                    onRegister?.Invoke(state);
                    Close();
                });

            UpdateInformation(item.FungibleAssetValue, onClose);
            base.Show();
            StartCoroutine(CoUpdate(sell.gameObject));
        }

        public void Show(
            InventoryItem item,
            System.Action onRegister,
            System.Action onClose)
        {
            registerButton.gameObject.SetActive(true);
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            _onRegister = onRegister;
            UpdateInformation(item.FungibleAssetValue, onClose);
            base.Show();
            StartCoroutine(CoUpdate(registerButton.gameObject));
        }

        private void UpdateInformation(FungibleAssetValue fav, System.Action onClose)
        {
            var grade = 1;
            var isRune = false;
            var id = 0;
            var ticker = fav.Currency.Ticker;
            if (RuneFrontHelper.TryGetRuneData(ticker, out var runeData))
            {
                var sheet = Game.Game.instance.TableSheets.RuneListSheet;
                if (sheet.TryGetValue(runeData.id, out var row))
                {
                    grade = row.Grade;
                    isRune = true;
                    id = runeData.id;
                }
            }

            var petSheet = Game.Game.instance.TableSheets.PetSheet;
            var petRow = petSheet.Values.FirstOrDefault(x => x.SoulStoneTicker == ticker);
            if (petRow is not null)
            {
                isRune = false;
                grade = petRow.Grade;
                id = petRow.Id;
            }

            fungibleAssetImage.sprite = fav.GetIconSprite();
            nameText.text = fav.GetLocalizedName();
            contentText.text = L10nManager.Localize($"ITEM_DESCRIPTION_{id}");
            var countFormat = L10nManager.Localize("UI_COUNT_FORMAT");
            countText.text = string.Format(countFormat, fav.GetQuantityString());
            levelLimitText.text = L10nManager.Localize("UI_REQUIRED_LEVEL", 1);
            UpdateGrade(grade);

            _onClose = onClose;
            scrollbar.value = 1f;
        }

        private void UpdateGrade(int grade)
        {
            var data = itemViewDataScriptableObject.GetItemViewData(grade);
            gradeImage.overrideSprite = data.GradeBackground;
            gradeHsv.range = data.GradeHsvRange;
            gradeHsv.hue = data.GradeHsvHue;
            gradeHsv.saturation = data.GradeHsvSaturation;
            gradeHsv.value = data.GradeHsvValue;

            var color = LocalizationExtensions.GetItemGradeColor(grade);
            gradeText.text = L10nManager.Localize($"UI_ITEM_GRADE_{grade}");
            gradeText.color = color;
            nameText.color = color;
            subTypeText.color = color;
            spacerImage.color = color;
        }

        protected IEnumerator CoUpdate(GameObject target)
        {
            var selectedGameObjectCache = TouchHandler.currentSelectedGameObject;
            while (selectedGameObjectCache is null)
            {
                selectedGameObjectCache = TouchHandler.currentSelectedGameObject;
                yield return null;
            }

            var positionCache = selectedGameObjectCache.transform.position;

            while (enabled)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _isClickedButtonArea = _isPointerOnScrollArea;
                }

                var current = TouchHandler.currentSelectedGameObject;
                if (current == selectedGameObjectCache)
                {
                    if (!Input.GetMouseButton(0) &&
                        Input.mouseScrollDelta == default)
                    {
                        yield return null;
                        continue;
                    }

                    if (!_isClickedButtonArea)
                    {
                        Close();
                        yield break;
                    }
                }
                else
                {
                    if (current == target)
                    {
                        yield break;
                    }

                    if (!_isClickedButtonArea)
                    {
                        Close();
                        yield break;
                    }
                }

                yield return null;
            }
        }

        public void OnEnterButtonArea(bool value)
        {
            _isPointerOnScrollArea = value;
        }
    }
}
