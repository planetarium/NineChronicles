using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.UI;
using Nekoyume.EnumType;
using Nekoyume.Pattern;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    [RequireComponent(typeof(Canvas))]
    public class MainCanvas : MonoSingleton<MainCanvas>
    {
        [SerializeField]
        private GameObject hudLayer = null;

        [SerializeField]
        private GameObject popupLayer = null;

        [SerializeField]
        private GameObject screenLayer = null;

        [SerializeField]
        private GameObject tooltipLayer = null;

        [SerializeField]
        private GameObject widgetLayer = null;

        [SerializeField]
        private GameObject animationLayer = null;

        [SerializeField]
        private GameObject systemInfoLayer = null;

        [SerializeField]
        private GameObject developmentLayer = null;

        private List<Widget> _firstWidgets;
        private List<Widget> _secondWidgets;

        public Canvas Canvas { get; private set; }
        public RectTransform RectTransform { get; private set; }

        public Transform GetTransform(WidgetType widgetType)
        {
            switch (widgetType)
            {
                case WidgetType.Hud:
                    return hudLayer.transform;
                case WidgetType.Popup:
                    return popupLayer.transform;
                case WidgetType.Screen:
                    return screenLayer.transform;
                case WidgetType.Tooltip:
                    return tooltipLayer.transform;
                case WidgetType.Widget:
                    return widgetLayer.transform;
                case WidgetType.Animation:
                    return animationLayer.transform;
                case WidgetType.SystemInfo:
                    return systemInfoLayer.transform;
                case WidgetType.Development:
                    return developmentLayer.transform;
                default:
                    throw new ArgumentOutOfRangeException(nameof(widgetType), widgetType, null);
            }
        }

        protected override void Awake()
        {
            base.Awake();

            Canvas = GetComponent<Canvas>();
            RectTransform = GetComponent<RectTransform>();
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
            // 임시로 팝업보다 상단에 배치
            _secondWidgets.Add(Widget.Create<BottomMenu>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Dialog>());
            yield return null;

            // 툴팁류.
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
        }

        public void SetSiblingOrderNext(WidgetType fromWidgetType, WidgetType targetWidgetType)
        {
            GameObject from = null;
            switch (fromWidgetType)
            {
                case WidgetType.Hud:
                    from = hudLayer;
                    break;
                case WidgetType.Popup:
                    from = popupLayer;
                    break;
                case WidgetType.Screen:
                    from = screenLayer;
                    break;
                case WidgetType.Tooltip:
                    from = tooltipLayer;
                    break;
                case WidgetType.Widget:
                    from = widgetLayer;
                    break;
                case WidgetType.SystemInfo:
                    from = systemInfoLayer;
                    break;
                case WidgetType.Development:
                    from = developmentLayer;
                    break;
            }

            GameObject target = null;
            switch (targetWidgetType)
            {
                case WidgetType.Hud:
                    target = hudLayer;
                    break;
                case WidgetType.Popup:
                    target = popupLayer;
                    break;
                case WidgetType.Screen:
                    target = screenLayer;
                    break;
                case WidgetType.Tooltip:
                    target = tooltipLayer;
                    break;
                case WidgetType.Widget:
                    target = widgetLayer;
                    break;
                case WidgetType.SystemInfo:
                    target = systemInfoLayer;
                    break;
                case WidgetType.Development:
                    target = developmentLayer;
                    break;
            }

            var fromIndex = from.transform.GetSiblingIndex();
            var targetIndex = target.transform.GetSiblingIndex();
            if (fromIndex > targetIndex)
            {
                from.transform.SetSiblingIndex(targetIndex + 1);
            }
            else
            {
                from.transform.SetSiblingIndex(targetIndex);
            }
        }
    }
}
