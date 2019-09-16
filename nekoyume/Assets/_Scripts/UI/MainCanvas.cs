using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
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

        private List<Widget> _widgets;

        public bool IsInitialized { get; private set; }
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
            IsInitialized = false;
            
            base.Awake();

            this.ComponentFieldsNotNullTest();

            Canvas = GetComponent<Canvas>();
            RectTransform = GetComponent<RectTransform>();
        }

        public void Initialize()
        {
            _widgets = new List<Widget>()
            {
                Widget.Create<Title>(),
                Widget.Create<Synopsis>(),
                Widget.Create<Login>(),
                Widget.Create<LoginDetail>(),
                Widget.Create<Menu>(),
                Widget.Create<Status>(),
                Widget.Create<Blind>(),
                Widget.Create<Shop>(),
                Widget.Create<QuestPreparation>(),
                Widget.Create<Combination>(),
                Widget.Create<RankingBoard>(),
                Widget.Create<WorldMap>(),
                Widget.Create<Dialog>(),
                Widget.Create<Battle>(),

                // 모듈류.
                Widget.Create<StatusDetail>(),
                Widget.Create<Inventory>(),
                Widget.Create<Quest>(),
                Widget.Create<Mail>(),

                // 팝업류.
                Widget.Create<BattleResult>(),
                Widget.Create<SimpleItemCountPopup>(),
                Widget.Create<ItemCountAndPricePopup>(),
                Widget.Create<CombinationResultPopup>(),
                Widget.Create<StageTitle>(),
                Widget.Create<Alert>(),
                Widget.Create<Confirm>(),

                // 툴팁류.
                Widget.Create<ItemInformationTooltip>(),

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

            foreach(var widget in _widgets)
            {
                widget.Initialize();
            }
            
            Notification.RegisterWidgetTypeForUX<Mail>();

            IsInitialized = true;
        }
    }
}
