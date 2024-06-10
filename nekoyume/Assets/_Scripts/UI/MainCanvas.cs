using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Game;
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
        private List<Widget> _secondWidgets = new();
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
            // Agent 초기화가 필요없는 widget && Agent 초기화 (로그인 포함)를 위해 필요한 widget
            var firstWidgets = new List<Widget>
            {
                // 스크린 영역. 로딩창류.
                Widget.Create<GrayLoadingScreen>(),
                Widget.Create<DimmedLoadingScreen>(),
                Widget.Create<LoadingScreen>(),

                // 팝업 영역.
                Widget.Create<SettingPopup>(),

                // 팝업 영역: 알림.
                Widget.Create<LoginSystem>(),
                Widget.Create<TitleOneButtonSystem>(),
                Widget.Create<Alert>(),

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

        public IEnumerator CreateSecondWidgets()
        {
            // 실제 모바일 환경이 아닌경우 사용되는 UI가 포함될 수는 있지만, 감안하고 플래그 사용
#if APPLY_MEMORY_IOS_OPTIMIZATION || UNITY_ANDROID || UNITY_IOS
            _secondWidgets.Add(Widget.Create<Login>());
            yield return null;
            _secondWidgets.Add(Widget.Create<LoginDetail>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Menu>());
            yield return null;

            _secondWidgets.Add(Widget.Create<MobileShop>());
            yield return null;

            _secondWidgets.Add(Widget.Create<WorldMap>());
            yield return null;

            _secondWidgets.Add(Widget.Create<BattlePreparation>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ArenaBattlePreparation>());
            yield return null;
            _secondWidgets.Add(Widget.Create<RaidPreparation>());
            yield return null;

            // module
            _secondWidgets.Add(Widget.Create<Craft>());
            _secondWidgets.Add(Widget.Create<Grind>());
            yield return null;

            // header menu
            _secondWidgets.Add(Widget.Create<HeaderMenuStatic>());
            // Popup included in header menu
            _secondWidgets.Add(Widget.Create<MailPopup>());
            _secondWidgets.Add(Widget.Create<QuestPopup>());
            _secondWidgets.Add(Widget.Create<AvatarInfoPopup>());
            _secondWidgets.Add(Widget.Create<CombinationSlotsPopup>());
            _secondWidgets.Add(Widget.Create<RankPopup>());
            _secondWidgets.Add(Widget.Create<ChatPopup>());
            _secondWidgets.Add(Widget.Create<QuitSystem>());
            _secondWidgets.Add(Widget.Create<BuffBonusPopup>());
            yield return null;

            _secondWidgets.Add(Widget.Create<SweepPopup>());
            yield return null;

            // tooltip
            _secondWidgets.Add(Widget.Create<MessageCatTooltip>(true));
            yield return null;

            _secondWidgets.Add(Widget.Create<Tutorial>());
            yield return null;
#else
            // Agent 초기화가 필요없는 widget
            _secondWidgets.Add(Widget.Create<BuffBonusLoadingScreen>());
            yield return null;
            _secondWidgets.Add(Widget.Create<WorldBossRewardScreen>());
            yield return null;
            _secondWidgets.Add(Widget.Create<RuneCombineResultScreen>());
            yield return null;
            _secondWidgets.Add(Widget.Create<RuneEnhancementResultScreen>());
            yield return null;
            _secondWidgets.Add(Widget.Create<MailRewardScreen>());
            yield return null;
            _secondWidgets.Add(Widget.Create<RewardScreen>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CPScreen>());
            yield return null;
            _secondWidgets.Add(Widget.Create<PetLevelUpResultScreen>());
            yield return null;
            _secondWidgets.Add(Widget.Create<PetSummonResultScreen>());
            yield return null;

            // 일반.
            _secondWidgets.Add(Widget.Create<Synopsis>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Login>());
            yield return null;
            _secondWidgets.Add(Widget.Create<LoginDetail>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Menu>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ArenaBattleLoadingScreen>());
            yield return null;
            // 메뉴보단 더 앞에 나와야 합니다.
            _secondWidgets.Add(Widget.Create<Battle>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ArenaBattle>());
            yield return null;
            _secondWidgets.Add(Widget.Create<WorldBossBattle>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Blind>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Status>());
            yield return null;
            _secondWidgets.Add(Widget.Create<EventBanner>());
            yield return null;

            _secondWidgets.Add(Widget.Create<ShopSell>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ShopBuy>());
            yield return null;
            _secondWidgets.Add(Widget.Create<WorldMap>());
            yield return null;
            _secondWidgets.Add(Widget.Create<StageInformation>());
            yield return null;
            _secondWidgets.Add(Widget.Create<WorldBoss>());
            yield return null;
            _secondWidgets.Add(Widget.Create<WorldBossDetail>());
            yield return null;
            _secondWidgets.Add(Widget.Create<BattlePreparation>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ArenaBattlePreparation>());
            yield return null;
            _secondWidgets.Add(Widget.Create<RaidPreparation>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Status>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ArenaJoin>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ArenaBoard>());
            yield return null;
            _secondWidgets.Add(Widget.Create<PatrolRewardPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<SeasonPassNewPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<EventReleaseNotePopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<SeasonPass>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Collection>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CollectionItemFilterPopup>());
            yield return null;

            // loading
            _secondWidgets.Add(Widget.Create<StageLoadingEffect>());
            yield return null;

            // module
            _secondWidgets.Add(Widget.Create<StageTitle>());
            _secondWidgets.Add(Widget.Create<CombinationMain>());
            _secondWidgets.Add(Widget.Create<Craft>());
            _secondWidgets.Add(Widget.Create<Enhancement>());
            _secondWidgets.Add(Widget.Create<Grind>());
            _secondWidgets.Add(Widget.Create<Rune>());
            _secondWidgets.Add(Widget.Create<Summon>());
            _secondWidgets.Add(Widget.Create<DccMain>());
            _secondWidgets.Add(Widget.Create<DccCollection>());
            yield return null;

            // header menu
            _secondWidgets.Add(Widget.Create<HeaderMenuStatic>());
            // Popup included in header menu
            _secondWidgets.Add(Widget.Create<MailPopup>());
            _secondWidgets.Add(Widget.Create<QuestPopup>());
            _secondWidgets.Add(Widget.Create<AvatarInfoPopup>());
            _secondWidgets.Add(Widget.Create<CombinationSlotsPopup>());
            _secondWidgets.Add(Widget.Create<RankPopup>());
            _secondWidgets.Add(Widget.Create<ChatPopup>());
            _secondWidgets.Add(Widget.Create<QuitSystem>());
            _secondWidgets.Add(Widget.Create<BuffBonusPopup>());
            yield return null;

            // Over than HeaderMenu
            _secondWidgets.Add(Widget.Create<RankingBattleResultPopup>());
            _secondWidgets.Add(Widget.Create<ItemCountAndPricePopup>());
            _secondWidgets.Add(Widget.Create<InputBoxPopup>());
            _secondWidgets.Add(Widget.Create<MonsterCollectionRewardsPopup>());
            _secondWidgets.Add(Widget.Create<CombinationResultPopup>());
            _secondWidgets.Add(Widget.Create<EnhancementResultPopup>());
            _secondWidgets.Add(Widget.Create<BattleResultPopup>());
            _secondWidgets.Add(Widget.Create<ItemCountableAndPricePopup>());
            _secondWidgets.Add(Widget.Create<WorldBossResultPopup>());
            yield return null;

            // popup
            _secondWidgets.Add(Widget.Create<CombinationSlotPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<BuyItemInformationPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<BuyFungibleAssetInformationPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<DialogPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CodeRewardPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<PrologueDialogPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CombinationLoadingScreen>());
            yield return null;
            _secondWidgets.Add(Widget.Create<GrindingLoadingScreen>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ConfirmPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CelebratesPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<WorldClearPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<FriendInfoPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<LevelUpCelebratePopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<PaymentPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ReplaceMaterialPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<SweepPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<BoosterPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<SweepResultPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<StakingPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<BuffBonusResultPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<SuperCraftPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<TicketPurchasePopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<PetEnhancementPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<MaterialNavigationPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ArenaTicketPurchasePopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ItemMaterialSelectPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ArenaTicketPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<DccSettingPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<PetSelectionPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ProfileSelectPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CostTwoButtonPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ConfirmConnectPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<SummonResultPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<SummonDetailPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<SummonSkillsPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ShopListPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<SeasonPassPremiumPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<InviteFriendsPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<StatsBonusPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CollectionRegistrationPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CollectionResultPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<RuneLevelBonusEffectPopup>());
            yield return null;

            // tooltip
            _secondWidgets.Add(Widget.Create<EquipmentTooltip>());
            _secondWidgets.Add(Widget.Create<ConsumableTooltip>());
            _secondWidgets.Add(Widget.Create<MaterialTooltip>());
            _secondWidgets.Add(Widget.Create<CostumeTooltip>());
            _secondWidgets.Add(Widget.Create<RuneTooltip>());
            _secondWidgets.Add(Widget.Create<FungibleAssetTooltip>());
            _secondWidgets.Add(Widget.Create<HelpTooltip>());
            _secondWidgets.Add(Widget.Create<VanilaTooltip>());
            _secondWidgets.Add(Widget.Create<MessageCatTooltip>(true));
            yield return null;

            // tutorial
            _secondWidgets.Add(Widget.Create<Tutorial>());
            yield return null;
#endif
        }

        public IEnumerator InitializeSecondWidgets()
        {
            Widget last = null;
            foreach (var value in _secondWidgets)
            {
                if (value is null)
                {
                    NcDebug.LogWarning($"value is null. last is {last.name}");
                    continue;
                }

                value.Initialize();
                yield return null;
                last = value;
            }
            Widgets.AddRange(_secondWidgets);
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

        // DevCra - iOS Memory Optimization
        public T AddWidget<T>() where T : Widget
        {
            var widget = Widget.Create<T>();
            _secondWidgets.Add(widget);
            widget.Initialize();
            Game.Game.instance.Stage.TutorialController?.RegisterWidget(widget);
            return widget;
        }

        // DevCra - iOS Memory Optimization
        public bool RemoveWidget<T>(T widget) where T : Widget
        {
            if (Widget.TryFind<T>(out var found))
            {
                if (found == widget)
                {
                    _secondWidgets.Remove(widget);
                    Game.Game.instance.Stage.TutorialController?.UnregisterWidget(widget);
                    return Widget.Remove(widget);
                }
            }

            return false;
        }
    }
}
