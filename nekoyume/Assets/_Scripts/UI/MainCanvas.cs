using UnityEngine;

namespace Nekoyume.UI
{
    [RequireComponent(typeof(Canvas))]
    public class MainCanvas : MonoSingleton<MainCanvas>
    {   
        public GameObject hud;
        public GameObject widget;
        public GameObject popup;
        
        public Canvas Canvas { get; private set; }
        public RectTransform RectTransform { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();
            
            this.ComponentFieldsNotNullTest();

            Canvas = GetComponent<Canvas>();
            RectTransform = GetComponent<RectTransform>();
            
            Widget.Create<Login>(true);
            Widget.Create<LoginDetail>();
            Widget.Create<Menu>();
            Widget.Create<Status>();
            Widget.Create<Blind>();
            Widget.Create<Shop>();
            Widget.Create<QuestPreparation>();
            Widget.Create<Combination>();
            
            // 모듈류.
            Widget.Create<StatusDetail>();
            Widget.Create<BattleResult>();
            Widget.Create<Inventory>();
            Widget.Create<Gold>();

            // 팝업류.
            Widget.Create<SelectItemCountPopup>();
            Widget.Create<CombinationResultPopup>();
            
            // 로딩창류.
            Widget.Create<GrayLoadingScreen>();
            Widget.Create<LoadingScreen>();
            Widget.Create<StageLoadingScreen>();
#if DEBUG
            Widget.Create<Cheat>(true);
#endif
        }
    }
}
