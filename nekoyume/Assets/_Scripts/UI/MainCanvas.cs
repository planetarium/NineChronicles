using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Pattern;
using UnityEngine;

namespace Nekoyume.UI
{
    [RequireComponent(typeof(Canvas))]
    public class MainCanvas : MonoSingleton<MainCanvas>
    {
        public GameObject hud;
        public GameObject popup;
        public GameObject screen;
        public GameObject tooltip;
        public GameObject widget;
        public GameObject systemInfo;

        private List<Widget> _firstWidgets;
        private List<Widget> _secondWidgets;

        public Canvas Canvas { get; private set; }
        public RectTransform RectTransform { get; private set; }

        public Transform GetTransform(WidgetType widgetType)
        {
            switch (widgetType)
            {
                case WidgetType.Hud:
                    return hud.transform;
                case WidgetType.Popup:
                    return popup.transform;
                case WidgetType.Screen:
                    return screen.transform;
                case WidgetType.Tooltip:
                    return tooltip.transform;
                case WidgetType.Widget:
                    return widget.transform;
                case WidgetType.SystemInfo:
                    return systemInfo.transform;
                default:
                    throw new ArgumentOutOfRangeException(nameof(widgetType), widgetType, null);
            }
        }

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            Canvas = GetComponent<Canvas>();
            RectTransform = GetComponent<RectTransform>();
        }

        public void InitializeFirst()
        {
            _firstWidgets = new List<Widget>
            {
                // 스크린 영역. 로딩창류.
                Widget.Create<GrayLoadingScreen>(),
                Widget.Create<LoadingScreen>(),
                Widget.Create<StageLoadingScreen>(),
                Widget.Create<CombinationLoadingScreen>(),

                //최상단 알림 영역.
                Widget.Create<UpdatePopup>(),
                Widget.Create<ExitPopup>(),
                Widget.Create<ActionFailPopup>(),
                Widget.Create<Notification>(true),
#if DEBUG
                Widget.Create<Cheat>(true),
#endif
            };

            foreach (var widget in _firstWidgets)
            {
                widget.Initialize();
            }

            Notification.RegisterWidgetTypeForUX<Mail>();
        }

        public IEnumerator InitializeSecond()
        {
            _secondWidgets = new List<Widget>();
            
            // 툴팁류.
            _secondWidgets.Add(Widget.Create<ItemInformationTooltip>());
            yield return null;
            
            // 일반.
            _secondWidgets.Add(Widget.Create<Title>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Synopsis>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Login>());
            yield return null;
            _secondWidgets.Add(Widget.Create<LoginDetail>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Menu>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Status>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Blind>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Shop>());
            yield return null;
            _secondWidgets.Add(Widget.Create<QuestPreparation>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Combination>());
            yield return null;
            _secondWidgets.Add(Widget.Create<RankingBoard>());
            yield return null;
            _secondWidgets.Add(Widget.Create<WorldMap>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Dialog>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Battle>());
            yield return null;

            // 모듈류.
            _secondWidgets.Add(Widget.Create<StatusDetail>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Inventory>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Quest>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Mail>());
            yield return null;

            // 팝업류.
            _secondWidgets.Add(Widget.Create<BattleResult>());
            yield return null;
            _secondWidgets.Add(Widget.Create<SimpleItemCountPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<ItemCountAndPricePopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<CombinationResultPopup>());
            yield return null;
            _secondWidgets.Add(Widget.Create<StageTitle>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Alert>());
            yield return null;
            _secondWidgets.Add(Widget.Create<Confirm>());
            yield return null;

            foreach (var widget in _secondWidgets)
            {
                widget.Initialize();
            }

            Notification.RegisterWidgetTypeForUX<Mail>();
        }
    }
}
