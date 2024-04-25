using System;
using System.Linq;
using System.Numerics;
using Coffee.UIEffects;
using Lib9c;
using Libplanet.Types.Assets;
using Nekoyume.Action;
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
        protected ItemTooltipDetail detail;

        [SerializeField]
        private ItemTooltipBuy buy;

        [SerializeField]
        private ItemTooltipSell sell;

        [SerializeField]
        private ConditionalButton registerButton;

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
            registerButton.OnClickSubject.Subscribe(_ =>
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
            string ticker,
            string amount,
            System.Action onClose)
        {
            sell.gameObject.SetActive(false);
            buy.gameObject.SetActive(false);

            UpdateInformation(ticker, amount, onClose);
            base.Show();
            StartCoroutine(CoUpdate(panel.gameObject));
        }

        public virtual void Show(
            FungibleAssetValue fav,
            System.Action onClose)
        {
            sell.gameObject.SetActive(false);
            buy.gameObject.SetActive(false);

            UpdateInformation(fav, onClose);
            base.Show();
            StartCoroutine(CoUpdate(panel.gameObject));
        }

        public virtual void Show(
            InventoryItem item,
            System.Action onClose)
        {
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
            buy.gameObject.SetActive(true);
            sell.gameObject.SetActive(false);
            buy.Set((BigInteger)item.FungibleAssetProduct.Price * States.Instance.GoldBalanceState.Gold.Currency,
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
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(true);
            sell.Set(apStoneCount,
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
            buy.gameObject.SetActive(false);
            sell.gameObject.SetActive(false);
            _onRegister = onRegister;
            UpdateInformation(item.FungibleAssetValue, onClose, true);
            base.Show();
            StartCoroutine(CoUpdate(registerButton.gameObject));
        }

        private void UpdateInformation(FungibleAssetValue fav, System.Action onClose, bool isAvailableSell = false)
        {
            var isTradeAble = fav.IsTradable();
            var grade = 1;
            var id = 0;
            var ticker = fav.Currency.Ticker;
            if (RuneFrontHelper.TryGetRuneData(ticker, out var runeData))
            {
                var sheet = Game.Game.instance.TableSheets.RuneListSheet;
                if (sheet.TryGetValue(runeData.id, out var row))
                {
                    grade = row.Grade;
                    id = runeData.id;
                }
            }

            var petSheet = Game.Game.instance.TableSheets.PetSheet;
            var petRow = petSheet.Values.FirstOrDefault(x => x.SoulStoneTicker == ticker);
            if (petRow is not null)
            {
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

            SetTradableState(isAvailableSell, isTradeAble, fav.MajorUnit > 0);
        }

        private void UpdateInformation(string ticker, string amount, System.Action onClose)
        {
            var tradable = true;
            var grade    = 1;
            var id       = 0;
            if (RuneFrontHelper.TryGetRuneData(ticker, out var runeData))
            {
                var sheet = Game.Game.instance.TableSheets.RuneListSheet;
                if (sheet.TryGetValue(runeData.id, out var row))
                {
                    grade = row.Grade;
                    id = runeData.id;
                }

                var rune = Currencies.GetRune(ticker);
                tradable = !RegisterProduct.NonTradableTickerCurrencies.Contains(rune);
            }

            var petSheet = Game.Game.instance.TableSheets.PetSheet;
            var petRow = petSheet.Values.FirstOrDefault(x => x.SoulStoneTicker == ticker);
            if (petRow is not null)
            {
                grade = petRow.Grade;
                id = petRow.Id;
            }

            fungibleAssetImage.sprite = SpriteHelper.GetFavIcon(ticker);
            nameText.text = LocalizationExtensions.GetLocalizedFavName(ticker);

            if (ticker.ToLower() == "crystal" || ticker.ToLower() == "fav_crystal")
            {
                contentText.text = L10nManager.Localize($"ITEM_DESCRIPTION_9999998");
                gradeText.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                contentText.text = L10nManager.Localize($"ITEM_DESCRIPTION_{id}");
                UpdateGrade(grade);
            }

            var countFormat = L10nManager.Localize("UI_COUNT_FORMAT");
            countText.text = string.Format(countFormat, amount);
            levelLimitText.text = L10nManager.Localize("UI_REQUIRED_LEVEL", 1);

            _onClose = onClose;
            scrollbar.value = 1f;

            SetTradableState(false, tradable);
        }

        private void UpdateGrade(int grade)
        {
            gradeText.transform.parent.gameObject.SetActive(true);

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

        private void SetTradableState(bool isAvailableSell, bool isTradable = false, bool hasItem = false)
        {
            registerButton.gameObject.SetActive(isAvailableSell);

            registerButton.Interactable = isTradable && isAvailableSell && hasItem;
            detail.UpdateTradableText(isTradable);
        }
    }
}
