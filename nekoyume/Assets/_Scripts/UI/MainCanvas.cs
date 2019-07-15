using System;
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

        public void Initialize()
        {
            Widget.Create<Title>();
            Widget.Create<Synopsis>();
            Widget.Create<Login>();
            Widget.Create<LoginDetail>();
            Widget.Create<Menu>();
            Widget.Create<Status>();
            Widget.Create<Blind>();
            Widget.Create<Shop>();
            Widget.Create<QuestPreparation>();
            Widget.Create<Combination>();
            Widget.Create<RankingBoard>();
            Widget.Create<WorldMap>();
            Widget.Create<Dialog>();

            // 모듈류.
            Widget.Create<StatusDetail>();
            Widget.Create<BattleResult>();
            Widget.Create<Inventory>();
            Widget.Create<Gold>();

            // 팝업류.
            Widget.Create<SimpleItemCountPopup>();
            Widget.Create<ItemCountAndPricePopup>();
            Widget.Create<CombinationResultPopup>();
            Widget.Create<StageTitle>();
            Widget.Create<Alert>();
            Widget.Create<Confirm>();
            
            // 툴팁류.
            Widget.Create<ItemInformationTooltip>();

            // 스크린 영역. 로딩창류.
            Widget.Create<GrayLoadingScreen>();
            Widget.Create<LoadingScreen>(true);
            Widget.Create<StageLoadingScreen>();
#if DEBUG
            Widget.Create<Cheat>(true);
#endif
        }
    }
}
