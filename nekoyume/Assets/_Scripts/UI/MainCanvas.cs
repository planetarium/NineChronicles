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
        private CanvasLayer popupLayer = default;

        [SerializeField]
        private CanvasLayer screenLayer = default;

        [SerializeField]
        private CanvasLayer tooltipLayer = default;

        [SerializeField]
        private CanvasLayer widgetLayer = default;

        [SerializeField]
        private CanvasLayer animationLayer = default;

        [SerializeField]
        private CanvasLayer systemInfoLayer = default;

        [SerializeField]
        private CanvasLayer developmentLayer = default;

        [SerializeField]
        private CanvasLayer tutorialMaskLayer = default;

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
                case WidgetType.Hud:
                    return hudLayer;
                case WidgetType.Popup:
                    return popupLayer;
                case WidgetType.Screen:
                    return screenLayer;
                case WidgetType.Tooltip:
                    return tooltipLayer;
                case WidgetType.Widget:
                    return widgetLayer;
                case WidgetType.Animation:
                    return animationLayer;
                case WidgetType.SystemInfo:
                    return systemInfoLayer;
                case WidgetType.Development:
                    return developmentLayer;
                case WidgetType.TutorialMask:
                    return tutorialMaskLayer;
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
                    tooltipLayer,
                    widgetLayer,
                    animationLayer,
                    systemInfoLayer,
                    developmentLayer,
                    tutorialMaskLayer,
                };
            }

            _layers = _layers.OrderBy(layer => layer.root.sortingOrder).ToList();
        }

        public void InitializeIntro()
        {
            var intro = Widget.Create<Intro>(true);
            intro.Initialize();

            UpdateLayers();
        }

        public void InitializeFirst()
        {
            var firstWidgets = new List<Widget>
            {
                // 스크린 영역. 로딩창류.
                Widget.Create<GrayLoadingScreen>(),
                Widget.Create<BlockSyncLoadingScreen>(),
                Widget.Create<LoadingScreen>(),
                Widget.Create<DataLoadingScreen>(),
                Widget.Create<PreloadingScreen>(),

                // 팝업 영역.
                Widget.Create<Settings>(),

                // 팝업 영역: 알림.
                Widget.Create<UpdatePopup>(),
                Widget.Create<BlockFailPopup>(),
                Widget.Create<ActionFailPopup>(),
                Widget.Create<LoginPopup>(),
                Widget.Create<SystemPopup>(),

                // 시스템 정보 영역.
                Widget.Create<BlockChainMessageBoard>(true),
                Widget.Create<Notification>(true),
                Widget.Create<OneLinePopup>(true),
                Widget.Create<VersionInfo>(true),
                Widget.Create<OneButtonPopup>(),
                Widget.Create<TwoButtonPopup>(),
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
            secondWidgets.Add(Widget.Create<RankingBattleLoadingScreen>());
            yield return null;
            secondWidgets.Add(Widget.Create<ArenaBattleLoadingScreen>());
            yield return null;
            // 메뉴보단 더 앞에 나와야 합니다.
            secondWidgets.Add(Widget.Create<VanilaTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<Battle>());
            yield return null;
            secondWidgets.Add(Widget.Create<Blind>());
            yield return null;
            secondWidgets.Add(Widget.Create<ShopSell>());
            yield return null;
            secondWidgets.Add(Widget.Create<ShopBuy>());
            yield return null;
            secondWidgets.Add(Widget.Create<WorldMap>());
            yield return null;
            secondWidgets.Add(Widget.Create<StageInformation>());
            yield return null;
            secondWidgets.Add(Widget.Create<QuestPreparation>());
            yield return null;
            secondWidgets.Add(Widget.Create<Status>());
            yield return null;
            secondWidgets.Add(Widget.Create<RankingBoard>());
            yield return null;
            secondWidgets.Add(Widget.Create<MimisbrunnrPreparation>());
            yield return null;
            secondWidgets.Add(Widget.Create<EventBanner>());
            yield return null;
            secondWidgets.Add(Widget.Create<CodeRewardButton>());
            yield return null;

            // loading
            secondWidgets.Add(Widget.Create<StageLoadingScreen>());
            yield return null;

            // module
            secondWidgets.Add(Widget.Create<StageTitle>());
            yield return null;
            secondWidgets.Add(Widget.Create<CombinationMain>());
            yield return null;
            secondWidgets.Add(Widget.Create<Craft>());
            yield return null;
            secondWidgets.Add(Widget.Create<UpgradeEquipment>());
            yield return null;

            // popup
            secondWidgets.Add(Widget.Create<RankingBattleResult>());
            yield return null;
            secondWidgets.Add(Widget.Create<ItemCountAndPricePopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<Alert>());
            yield return null;
            secondWidgets.Add(Widget.Create<InputBox>());
            yield return null;
            secondWidgets.Add(Widget.Create<LevelUpCelebratePopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<MonsterCollectionRewardsPopup>());
            yield return null;

            // header menu
            secondWidgets.Add(Widget.Create<HeaderMenu>());
            yield return null;

            // Popup included in header menu
            secondWidgets.Add(Widget.Create<Mail>());
            yield return null;
            secondWidgets.Add(Widget.Create<Quest>());
            yield return null;
            secondWidgets.Add(Widget.Create<AvatarInfo>());
            yield return null;
            secondWidgets.Add(Widget.Create<CombinationSlots>());
            yield return null;
            secondWidgets.Add(Widget.Create<Rank>());
            yield return null;
            secondWidgets.Add(Widget.Create<ChatPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<QuitPopup>());
            yield return null;

            // Over than HeaderMenu
            secondWidgets.Add(Widget.Create<CombinationResult>());
            yield return null;
            secondWidgets.Add(Widget.Create<EnhancementResult>());
            yield return null;
            secondWidgets.Add(Widget.Create<BattleResult>());
            yield return null;
            secondWidgets.Add(Widget.Create<ItemCountableAndPricePopup>());
            yield return null;

            // popup
            secondWidgets.Add(Widget.Create<CombinationSlotPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<CombinationResultPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<FriendInfoPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<ItemInformationTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<Dialog>());
            yield return null;
            secondWidgets.Add(Widget.Create<CodeReward>());
            yield return null;
            secondWidgets.Add(Widget.Create<DailyRewardItemPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<PrologueDialog>());
            yield return null;
            secondWidgets.Add(Widget.Create<CombinationLoadingScreen>());
            yield return null;
            secondWidgets.Add(Widget.Create<CelebratesPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<HelpPopup>());
            yield return null;
            secondWidgets.Add(Widget.Create<Confirm>());

            // tooltip
            secondWidgets.Add(Widget.Create<AvatarTooltip>());
            yield return null;
            secondWidgets.Add(Widget.Create<MessageCatManager>(true));
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

            Widget.Find<Settings>().transform.SetAsLastSibling();
        }

        public void SetLayerSortingOrderToTarget(
            WidgetType fromWidgetType,
            WidgetType toWidgetType,
            bool checkFromIsSmallerThanTo = true)
        {
            if (fromWidgetType == toWidgetType)
            {
                return;
            }

            var from = GetLayer(fromWidgetType);
            var fromSortingOrder = from.root.sortingOrder;
            var to = GetLayer(toWidgetType);
            var toSortingOrder = to.root.sortingOrder;
            if (fromSortingOrder == toSortingOrder)
            {
                return;
            }

            if (checkFromIsSmallerThanTo &&
                fromSortingOrder > toSortingOrder)
            {
                return;
            }

            var fromIndex = fromSortingOrder == 0 ? 0 : fromSortingOrder / 10;
            var toIndex = toSortingOrder == 0 ? 0 : toSortingOrder / 10;
            from.SetSortingOrder(toSortingOrder);

            if (fromIndex < toIndex)
            {
                for (var i = fromIndex + 1; i < toIndex + 1; i++)
                {
                    var layer = _layers[i];
                    layer.SetSortingOrder(layer.root.sortingOrder - 10);
                }
            }
            else
            {
                for (var i = toIndex; i < fromIndex; i++)
                {
                    var layer = _layers[i];
                    layer.SetSortingOrder(layer.root.sortingOrder + 10);
                }
            }

            UpdateLayers();
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
