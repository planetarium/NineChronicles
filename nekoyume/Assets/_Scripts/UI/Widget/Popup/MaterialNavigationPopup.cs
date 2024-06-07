using System;
using Coffee.UIEffects;
using Nekoyume.EnumType;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class MaterialNavigationPopup : PopupWidget
    {
        [Serializable]
        private struct SubItemCount
        {
            public GameObject container;
            public Image icon;
            public TextMeshProUGUI countText;
        }

        [Serializable]
        private struct BlockGauge
        {
            public GameObject container;
            public Image gaugeFillImage;
            public GameObject filledEffectImage;
            public TextMeshProUGUI minText;
            public TextMeshProUGUI maxText;
            public TextMeshProUGUI remainBlockText;
            public BonusItem bonusItem;
        }

        [Serializable]
        private struct BonusItem
        {
            public GameObject container;
            public Image icon;
            public TextMeshProUGUI countText;
            public Button button;
        }

        [Serializable]
        private struct InfoText
        {
            public GameObject container;
            public UIHsvModifier hsvModifier;
            public TextMeshProUGUI infoText;
        }

        [SerializeField]
        private CostIconDataScriptableObject costIconData;

        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private TextMeshProUGUI itemNameText;

        [SerializeField]
        private TextMeshProUGUI itemCountText;

        [SerializeField]
        private SubItemCount subItem;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private BlockGauge blockGauge;

        [SerializeField]
        private InfoText infoText;

        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private Button actionButton;

        [SerializeField]
        private TextMeshProUGUI actionButtonText;

        [SerializeField]
        private ConditionalButton conditionalButtonBrown;

        [SerializeField]
        private ConditionalButton conditionalButtonYellow;

        private System.Action _callback;
        private System.Action _chargeAP;
        private System.Action _getDailyReward;

        protected override void Awake()
        {
            base.Awake();

            confirmButton.onClick.AddListener(() => Close(true));
            actionButton.onClick.AddListener(() =>
            {
                Close(true);
                _callback?.Invoke();
            });
            conditionalButtonBrown.OnSubmitSubject.Subscribe(_ => InvokeAfterActionPointCheck(() =>
            {
                Close();
                _chargeAP?.Invoke();
            }));
            conditionalButtonYellow.OnSubmitSubject.Subscribe(_ => InvokeAfterActionPointCheck(() =>
            {
                Close();
                _getDailyReward?.Invoke();
            }));
        }

        private static void InvokeAfterActionPointCheck(System.Action action)
        {
            if (ReactiveAvatarState.ActionPoint > 0)
            {
                ActionPoint.ShowRefillConfirmPopup(action);
            }
            else
            {
                action();
            }
        }

        public void Show(
            System.Action callback,
            Sprite itemIcon,
            string itemName,
            string itemCount,
            string content,
            string buttonText)
        {
            _callback = callback;
            itemImage.sprite = itemIcon;
            itemNameText.text = itemName;
            var split = itemCount.Split('.');
            itemCountText.text = L10nManager.Localize("UI_COUNT_FORMAT", split[0]);
            contentText.text = content;
            actionButtonText.text = buttonText;
            infoText.infoText.gameObject.SetActive(infoText.infoText.text != string.Empty);
            infoText.container.SetActive(false);  // set default
            subItem.container.SetActive(false);
            blockGauge.container.SetActive(false);
            actionButton.gameObject.SetActive(true);
            conditionalButtonBrown.gameObject.SetActive(false);
            conditionalButtonYellow.gameObject.SetActive(false);
            base.Show();
        }

        public void SetInfo(bool isActive, (string, bool) value = default)
        {
            infoText.container.SetActive(isActive);
            var (info, isPositive) = value;
            infoText.infoText.text = info;
            infoText.infoText.color = isPositive
                ? Palette.GetColor(ColorType.TextPositive)
                : Palette.GetColor(ColorType.TextDenial);
            infoText.hsvModifier.enabled = isPositive;
        }

        public void ShowAP(
            string itemCount,
            int subItemCount,
            long blockRange,
            long maxBlockRange,
            bool isInteractable,
            System.Action chargeAP,
            System.Action getDailyReward,
            bool ignoreShowAnimation = false)
        {
            const CostType costType = CostType.ActionPoint;
            const int itemId = 9999996;
            itemImage.sprite = costIconData.GetIcon(costType);
            itemCountText.text = itemCount;
            itemNameText.text = L10nManager.Localize($"ITEM_NAME_{itemId}");
            contentText.text = L10nManager.Localize($"ITEM_DESCRIPTION_{itemId}");
            infoText.container.SetActive(false); // set default

            actionButton.gameObject.SetActive(false);
            conditionalButtonBrown.gameObject.SetActive(true);
            conditionalButtonYellow.gameObject.SetActive(true);

            const int subItemId = 500000;  // APStone
            subItem.container.SetActive(true);
            subItem.icon.sprite = SpriteHelper.GetItemIcon(subItemId);
            subItem.countText.text =
                $"{L10nManager.Localize($"ITEM_NAME_{subItemId}")} : {subItemCount}";

            const string bonusItemTicker = "RUNE_ADVENTURER";
            const int bonusItemCount = 1;
            blockGauge.maxText.text = maxBlockRange.ToString();
            blockGauge.bonusItem.icon.sprite = SpriteHelper.GetFavIcon(bonusItemTicker);
            blockGauge.bonusItem.countText.text = bonusItemCount.ToString();
            blockGauge.bonusItem.button.onClick.RemoveAllListeners();
            blockGauge.bonusItem.button.onClick.AddListener(() =>
            {
                Close();
                if (!RuneFrontHelper.TryGetRuneData(bonusItemTicker, out var runeData))
                {
                    return;
                }
                Find<MaterialNavigationPopup>().ShowRuneStone(runeData.id);
            });

            var remainBlockRange = maxBlockRange - blockRange;
            blockGauge.container.SetActive(true);
            blockGauge.gaugeFillImage.fillAmount = (float)blockRange / maxBlockRange;
            blockGauge.filledEffectImage.SetActive(blockRange >= maxBlockRange);
            blockGauge.remainBlockText.text =
                $"{remainBlockRange:#,0}({remainBlockRange.BlockRangeToTimeSpanString()})";

            conditionalButtonBrown.Interactable = isInteractable && subItemCount > 0;
            conditionalButtonYellow.Interactable = isInteractable && remainBlockRange <= 0;
            _chargeAP = chargeAP;
            _getDailyReward = getDailyReward;

            base.Show(ignoreShowAnimation);
        }

        public void ShowCurrency(CostType costType)
        {
            int itemId;
            string count, buttonText;
            System.Action callback;
            switch (costType)
            {
                case CostType.NCG:
                    itemId = 9999999;
                    count = States.Instance.GoldBalanceState.Gold.GetQuantityString();
                    buttonText = L10nManager.Localize("GRIND_UI_BUTTON");
                    callback = () =>
                    {
                        if (BattleRenderer.Instance.IsOnBattle)
                        {
                            return;
                        }

                        CloseWithOtherWidgets();
                        Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
                        Find<Grind>().Show();
                    };
                    break;
                case CostType.Crystal:
                    itemId = 9999998;
                    count = States.Instance.CrystalBalance.GetQuantityString();
                    buttonText = L10nManager.Localize("GRIND_UI_BUTTON");
                    callback = () =>
                    {
                        if (BattleRenderer.Instance.IsOnBattle)
                        {
                            return;
                        }

                        CloseWithOtherWidgets();
                        Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Combination);
                        Find<Grind>().Show();
                    };
                    break;
                case CostType.Hourglass:
                    itemId = 9999997;
                    var hourglassCount = Util.GetHourglassCount(
                        States.Instance.CurrentAvatarState.inventory,
                        Game.Game.instance.Agent.BlockIndex);
                    count = hourglassCount.ToString();
                    buttonText = L10nManager.Localize("UI_COMBINATION");
                    callback = () => { Find<CombinationSlotsPopup>().Show(); };
                    break;
                case CostType.SilverDust:
                case CostType.GoldDust:
                case CostType.RubyDust:
                    itemId = (int)costType;
                    var materialCount =
                        States.Instance.CurrentAvatarState.inventory.GetMaterialCount(itemId);
                    count = materialCount.ToString();

                    if (costType == CostType.SilverDust)
                    {
                        buttonText = L10nManager.Localize("UI_PATROL_REWARD");
                        callback = () =>
                        {
                            if (BattleRenderer.Instance.IsOnBattle)
                            {
                                return;
                            }

                            CloseWithOtherWidgets();
                            Game.Event.OnRoomEnter.Invoke(true);
                            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Main);
                            Find<PatrolRewardPopup>().Show();
                        };
                    }
                    else  // CostType.GoldDust
                    {
                        buttonText = L10nManager.Localize("UI_SHOP");
                        callback = () =>
                        {
                            if (BattleRenderer.Instance.IsOnBattle)
                            {
                                return;
                            }

                            CloseWithOtherWidgets();
                            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                            Find<ShopBuy>().Show();
                        };
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(costType), costType, null);
            }

            var icon = costIconData.GetIcon(costType);
            var itemName = L10nManager.Localize($"ITEM_NAME_{itemId}");
            var content = L10nManager.Localize($"ITEM_DESCRIPTION_{itemId}");

            SetInfo(false);
            Show(callback, icon, itemName, count, content, buttonText);
        }

        public void ShowRuneStone(int runeStoneId)
        {
            var itemName = L10nManager.Localize($"ITEM_NAME_{runeStoneId}");
            var content = L10nManager.Localize($"ITEM_DESCRIPTION_{runeStoneId}");

            var ticker = Game.Game.instance.TableSheets.RuneSheet[runeStoneId].Ticker;
            var icon = SpriteHelper.GetFavIcon(ticker);
            var count = States.Instance.CurrentAvatarBalances[ticker].GetQuantityString();

            string buttonText;
            System.Action callback;
            // Adventure's Rune
            if (runeStoneId == 30001)
            {
                buttonText = L10nManager.Localize("UI_CHARGE_AP");
                callback = () => Find<HeaderMenuStatic>().ActionPoint.ShowMaterialNavigationPopup();
                SetInfo(false);
            }
            // Golden leaf Rune(=> Shop), World Boss Rune
            else
            {
                var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                var isExist = RuneFrontHelper.TryGetRunStoneInformation(
                    currentBlockIndex,
                    runeStoneId,
                    out var info,
                    out var canObtain);

                buttonText = canObtain
                    ? L10nManager.Localize("UI_MAIN_MENU_WORLDBOSS")
                    : L10nManager.Localize("UI_SHOP");
                SetInfo(isExist, (info, canObtain));

                callback = () =>
                {
                    Find<Rune>().Close(true);
                    if (canObtain)
                    {
                        Find<WorldBoss>().ShowAsync().Forget();
                    }
                    else
                    {
                        Find<HeaderMenuStatic>()
                            .UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                        Find<ShopBuy>().Show();
                    }
                };
            }

            Show(callback, icon, itemName, count, content, buttonText);
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionActionPointChargeButton()
        {
            Close(true);
            _getDailyReward();
        }
    }
}
