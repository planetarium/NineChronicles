using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Scripts.UI;
using Nekoyume.EnumType;
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

            public IReadOnlyList<Canvas> Children { get; private set; }

            public void UpdateChildren()
            {
                Children = root.GetComponentsInChildren<Canvas>()
                    .Where(canvas => !canvas.Equals(root))
                    .ToList();
            }

            public void SetSortingOrder(int sortingOrder)
            {
                var rootSortingOrderBackup = root.sortingOrder;
                root.sortingOrder = sortingOrder;

                if (Children is null)
                {
                    UpdateChildren();
                }

                foreach (var childCanvas in Children)
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

        private List<CanvasLayer> _layers;
        private List<Widget> _firstWidgets;
        private List<Widget> _secondWidgets;

        public RectTransform RectTransform { get; private set; }
        public Canvas Canvas { get; private set; }

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
                };

                _layers = _layers.OrderBy(layer => layer.root.sortingOrder).ToList();
            }

            foreach (var layer in _layers)
            {
                layer.UpdateChildren();
            }
        }

        public void InitializeFirst()
        {
            _firstWidgets = new List<Widget>
            {
                // 스크린 영역. 로딩창류.
                Widget.Create<GrayLoadingScreen>(),
                Widget.Create<StageLoadingScreen>(),
                Widget.Create<LoadingScreen>(),
                Widget.Create<PreloadingScreen>(true),
                Widget.Create<Title>(true),

                // 팝업 영역.
                Widget.Create<Settings>(),
                Widget.Create<Confirm>(),

                // 팝업 영역: 알림.
                Widget.Create<UpdatePopup>(),
                Widget.Create<BlockFailPopup>(),
                Widget.Create<ActionFailPopup>(),
                Widget.Create<LoginPopup>(),
                Widget.Create<SystemPopup>(),

                // 시스템 정보 영역.
                Widget.Create<BlockChainMessageBoard>(true),
                Widget.Create<Notification>(true),
            };

            foreach (var value in _firstWidgets)
            {
                value.Initialize();
            }

            Notification.RegisterWidgetTypeForUX<Mail>();

            UpdateLayers();
        }

        public IEnumerator InitializeSecond()
        {
            _secondWidgets = new List<Widget>();

            _secondWidgets.Add(Widget.Create<ItemInformationTooltip>());
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
            _secondWidgets.Add(Widget.Create<RankingBattleLoadingScreen>());
            yield return null;
            // 메뉴보단 더 앞에 나와야 합니다.
            _secondWidgets.Add(Widget.Create<VanilaTooltip>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Battle>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Blind>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Shop>());
            yield return null;
            _secondWidgets.Add(Widget.Create<QuestPreparation>());
            yield return null;
            _secondWidgets.Add(Widget.Create<WorldMap>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Status>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Combination>());
            yield return null;
            _secondWidgets.Add(Widget.Create<RankingBoard>());
            yield return null;

            // 모듈류.
            _secondWidgets.Add(Widget.Create<StatusDetail>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Inventory>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Mail>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Quest>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CombinationSlots>());
            yield return null;
            _secondWidgets.Add(Widget.Create<AvatarInfo>());
            yield return null;

            // 팝업류.
            _secondWidgets.Add(Widget.Create<BattleResult>());
            yield return null;
            _secondWidgets.Add(Widget.Create<RankingBattleResult>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ItemCountAndPricePopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CombinationResultPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<StageTitle>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Alert>());
            yield return null;
            _secondWidgets.Add(Widget.Create<InputBox>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CombinationSlotPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<RedeemRewardPopup>());
            yield return null;
            // 임시로 팝업보다 상단에 배치
            _secondWidgets.Add(Widget.Create<BottomMenu>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Dialog>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CombinationLoadingScreen>());
            yield return null;

            // 툴팁류.
            _secondWidgets.Add(Widget.Create<AvatarTooltip>());
            yield return null;
            _secondWidgets.Add(Widget.Create<MessageCatManager>(true));
            yield return null;

            Widget last = null;
            foreach (var value in _secondWidgets)
            {
                if (value is null)
                {
                    Debug.LogWarning($"value is null. last is {last.name}");
                    continue;
                }

                value.Initialize();
                last = value;
            }

            Notification.RegisterWidgetTypeForUX<Mail>();

            UpdateLayers();
        }

        public void SetLayerSortingOrderToTarget(WidgetType fromWidgetType,
            WidgetType toWidgetType)
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

            _layers = _layers.OrderBy(layer => layer.root.sortingOrder).ToList();
        }
    }
}
