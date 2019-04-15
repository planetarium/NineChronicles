using UnityEngine;

namespace Nekoyume.UI
{
    public class MainCanvas : MonoSingleton<MainCanvas>
    {
        public GameObject hud;
        public GameObject widget;
        public GameObject popup;
        
        public RectTransform RectTransform { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();
            
            this.ComponentFieldsNotNullTest();

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

            // 팝업류.
            Widget.Create<SelectItemCountPopup>();
            Widget.Create<CombinationResultPopup>();
            
            // 로딩창류.
            Widget.Create<GrayLoadingScreen>();
            Widget.Create<LoadingScreen>();
#if DEBUG
            Widget.Create<Cheat>(true);
#endif
        }
    }
}
