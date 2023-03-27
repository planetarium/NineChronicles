using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.Pattern;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    [RequireComponent(typeof(Canvas)),
     RequireComponent(typeof(RectTransform))]
    public class MainCanvas : MonoSingleton<MainCanvas>
    {
        [Serializable]
        public class CanvasLayer
        {
            public Canvas root;

            public void SetSortingOrder(int sortingOrder)
            {
                var rootSortingOrderBackup = root.sortingOrder;
                if (rootSortingOrderBackup == sortingOrder)
                {
                    return;
                }

                root.sortingOrder = sortingOrder;

                foreach (var childCanvas in root.GetComponentsInChildren<Canvas>(true)
                    .Where(canvas => !canvas.Equals(root))
                    .ToList())
                {
                    childCanvas.sortingOrder =
                        sortingOrder + (childCanvas.sortingOrder - rootSortingOrderBackup);
                }
            }
        }

        [SerializeField]
        private CanvasGroup canvasGroup = null;

        [SerializeField]
        private CanvasLayer hudLayer = default;

        [SerializeField]
        private CanvasLayer widgetLayer = default;

        [SerializeField]
        private CanvasLayer staticLayer = default;

        [SerializeField]
        private CanvasLayer popupLayer = default;

        [SerializeField]
        private CanvasLayer animationLayer = default;

        [SerializeField]
        private CanvasLayer tooltipLayer = default;

        [SerializeField]
        private CanvasLayer tutorialMaskLayer = default;

        [SerializeField]
        private CanvasLayer screenLayer = default;

        [SerializeField]
        private CanvasLayer systemLayer = default;

        [SerializeField]
        private CanvasLayer developmentLayer = default;



        private List<CanvasLayer> _layers;
        public RectTransform RectTransform { get; private set; }
        public Canvas Canvas { get; private set; }
        public List<Widget> Widgets { get; private set; } = new List<Widget>();

        public bool Interactable
        {
            get => canvasGroup.interactable;
            set => canvasGroup.interactable = value;
        }

        public CanvasLayer GetLayer(WidgetType widgetType)
        {
            switch (widgetType)
            {
                // UI
                case WidgetType.Hud:
                    return hudLayer;
                case WidgetType.Widget:
                    return widgetLayer;
                case WidgetType.Static:
                    return staticLayer;
                case WidgetType.Popup:
                    return popupLayer;
                case WidgetType.Animation:
                    return animationLayer;
                case WidgetType.Tooltip:
                    return tooltipLayer;
                case WidgetType.TutorialMask:
                    return tutorialMaskLayer;
                case WidgetType.Screen:
                    return screenLayer;
            // SystemUI
                case WidgetType.System:
                    return systemLayer;
                case WidgetType.Development:
                    return developmentLayer;

                default:
                    throw new ArgumentOutOfRangeException(nameof(widgetType), widgetType, null);
            }
        }

        public Transform GetLayerRootTransform(WidgetType widgetType)
        {
            return GetLayer(widgetType).root.transform;
        }

        protected override void Awake()
        {
            base.Awake();

            RectTransform = GetComponent<RectTransform>();
            Canvas = GetComponent<Canvas>();
        }

        private void UpdateLayers()
        {
            if (_layers is null)
            {
                _layers = new List<CanvasLayer>
                {
                    hudLayer,
                    popupLayer,
                    screenLayer,
                    staticLayer,
                    tooltipLayer,
                    widgetLayer,
                    animationLayer,
                    systemLayer,
                    developmentLayer,
                    tutorialMaskLayer,
                };
            }

            _layers = _layers.OrderBy(layer => layer.root.sortingOrder).ToList();
        }

        public void InitializeIntro()
        {
            var intro = Widget.Create<IntroScreen>(true);
            intro.Initialize();

            UpdateLayers();
        }

        public void InitializeFirst()
        {
            var firstWidgets = new List<Widget>
            {
                // 스크린 영역. 로딩창류.
                Widget.Create<GrayLoadingScreen>(),
                Widget.Create<DimmedLoadingScreen>(),
                Widget.Create<LoadingScreen>(),
                Widget.Create<DataLoadingScreen>(),
                Widget.Create<WorldMapLoadingScreen>(),
                Widget.Create<UnlockWorldLoadingScreen>(),
                Widget.Create<BuffBonusLoadingScreen>(),
                Widget.Create<PreloadingScreen>(),
                Widget.Create<WorldBossRewardScreen>(),
                Widget.Create<RuneCombineResultScreen>(),
                Widget.Create<RuneEnhancementResultScreen>(),
                Widget.Create<MailRewardScreen>(),
                Widget.Create<CPScreen>(),
                Widget.Create<PetLevelUpResultScreen>(),
                Widget.Create<PetSummonResultScreen>(),

                // 팝업 영역.
                Widget.Create<SettingPopup>(),

                // 팝업 영역: 알림.
                Widget.Create<BlockFailTitleOneButtonSystem>(),
                Widget.Create<LoginSystem>(),
                Widget.Create<TitleOneButtonSystem>(),

                // 시스템 정보 영역.
                Widget.Create<OneButtonSystem>(),
                Widget.Create<TwoButtonSystem>(),
                Widget.Create<IconAndButtonSystem>(),
                Widget.Create<OneLineSystem>(true),
                Widget.Create<NotificationSystem>(true),
                Widget.Create<BlockChainMessageSystem>(true),
                Widget.Create<VersionSystem>(true),
            };

            foreach (var value in firstWidgets)
            {
                value.Initialize();
            }
            Widgets.AddRange(firstWidgets);

            UpdateLayers();
        }

        public IEnumerator InitializeSecond()
        {
            var secondWidgets = new List<Widget>();

            // 일반.
            secondWidgets.Add(Widget.Create<Synopsis>());
            yield return null;
            secondWidgets.Add(Widget.Create<Login>());
            yield return null;
            secondWidgets.Add(Widget.Create<LoginDetail>());
            yield return null;
            secondWidgets.Add(Widget.Create<Menu>());
            yield return null;
            secondWidgets.Add(Widget.Create<ArenaBattleLoadingScreen>());
            yield return null;
            // 메뉴보단 더 앞에 나와야 합니다.
            secondWidgets.Add(Widget.Create<Battle>());
            yield return null;
            secondWidgets.Add(Widget.Create<ArenaBattle>());
            yield return null;
            secondWidgets.Add(Widget.Create<WorldBossBattle>());
            yield return null;
            secondWidgets.Add(Widget.Create<Blind>());
            yield return null;
            secondWidgets.Add(Widget.Create<Status>());
            yield return null;
            secondWidgets.Add(Widget.Create<EventBanner>());
            yield return null;
            secondWidgets.Add(Widget.Create<ShopSell>());
            yield return null;
            secondWidgets.Add(Widget.Create<ShopBuy>());
            yield return null;
            secondWidgets.Add(Widget.Create<WorldMap>());
            yield return null;
            secondWidgets.Add(Widget.Create<StageInformation>());
            yield return null;
            secondWidgets.Add(Widget.Create<WorldBoss>());
            yield return null;
            secondWidgets.Add(Widget.Create<WorldBossDetail>());
            yield return null;
            secondWidgets.Add(Widget.Create<BattlePreparation>());
            yield return null;
            secondWidgets.Add(Widget.Create<ArenaBattlePreparation>());
            yield return null;
            secondWidgets.Add(Widget.Create<RaidPreparation>());
            yield return null;
            secondWidgets.Add(Widget.Create<DccMain>());
            yield return null;
            secondWidgets.Add(Widget.Create<DccCollection>());
            yield return null;
            secondWidgets.Add(Widget.Create<Status>());
            yield return null;
            secondWidgets.Add(Widget.Create<ArenaJoin>());
            yield return null;
            secondWidgets.Add(Widget.Create<ArenaBoard>());
            yield return null;
            secondWidgets.Add(Widget.Create<EventReleaseNotePopup>());
            yield return null;

            // loading
            secondWidgets.Add(Widget.Create<StageLoadingEffect>());
            yield return null;

            // module
            secondWidgets.Add(Widget.Create<StageTitle>());
            yield return null;
            secondWidgets.Add(Widget.Create<CombinationMain>());
            yield return null;
            secondWidgets.Add(Widget.Create<Craft>());
            yield return null;
            secondWidgets.Add(Widget.Create<Enhancement>());
            yield return null;
            secondWidgets.Add(Widget.Create<Grind>());
            yield return null;
            secondWidgets.Add(Widget.Create<Rune>());
            yield return null;

            // header menu
            secondWidgets.Add(Widget.Create<HeaderMenuStatic>());
            yield return null;

            // Popup included in header menu
            secondWidgets.Add(Widget.Create<MailPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<QuestPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<AvatarInfoPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<CombinationSlotsPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<RankPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<ChatPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<QuitSystem>());
            yield return null;
            secondWidgets.Add(Widget.Create<BuffBonusPopup>());
            yield return null;

            // Over than HeaderMenu
            secondWidgets.Add(Widget.Create<RankingBattleResultPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<ItemCountAndPricePopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<Alert>());
            yield return null;
            secondWidgets.Add(Widget.Create<InputBoxPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<MonsterCollectionRewardsPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<CombinationResultPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<EnhancementResultPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<BattleResultPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<ItemCountableAndPricePopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<WorldBossResultPopup>());
            yield return null;

            // popup
            secondWidgets.Add(Widget.Create<CombinationSlotPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<BuyItemInformationPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<BuyFungibleAssetInformationPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<DialogPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<CodeRewardPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<PrologueDialogPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<CombinationLoadingScreen>());
            yield return null;
            secondWidgets.Add(Widget.Create<GrindingLoadingScreen>());
            yield return null;
            secondWidgets.Add(Widget.Create<ConfirmPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<CelebratesPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<FriendInfoPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<LevelUpCelebratePopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<PaymentPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<ReplaceMaterialPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<SweepPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<BoosterPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<SweepResultPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<StakingPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<BuffBonusResultPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<SuperCraftPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<TicketPurchasePopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<PetEnhancementPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<MaterialNavigationPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<ArenaTicketPurchasePopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<ItemMaterialSelectPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<ArenaTicketPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<DccSettingPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<PetSelectionPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<ProfileSelectPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<CostTwoButtonPopup>());
            yield return null;

            // tooltip
            secondWidgets.Add(Widget.Create<EquipmentTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<ConsumableTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<MaterialTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<CostumeTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<RuneTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<FungibleAssetTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<AvatarTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<HelpTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<VanilaTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<MessageCatTooltip>(true));
            yield return null;

            // tutorial
            secondWidgets.Add(Widget.Create<Tutorial>());
            yield return null;

            Widget last = null;
            foreach (var value in secondWidgets)
            {
                if (value is null)
                {
                    Debug.LogWarning($"value is null. last is {last.name}");
                    continue;
                }

                value.Initialize();
                yield return null;
                last = value;
            }
            Widgets.AddRange(secondWidgets);
            UpdateLayers();

            Widget.Find<SettingPopup>().transform.SetAsLastSibling();
            EventManager.UpdateEventContainer(transform);
        }

        public void InitWidgetInMain()
        {
            var layer = widgetLayer.root.transform;
            for(int i = 0; i < layer.childCount; ++i)
            {
                var child = layer.GetChild(i);
                var widget = child.GetComponent<Widget>();
                if (widget is Status || widget is Menu)
                {
                    widget.Show();
                }
                else
                {
                    widget.Close();
                }
            }
        }
    }
}
