using UnityEngine;

namespace Nekoyume.UI
{
    public class MainCanvas : MonoBehaviour
    {
        private void Awake()
        {
            GameObject hudContainer = new GameObject("HUD");
            hudContainer.transform.parent = transform;
            GameObject widgetContainer = new GameObject("Widget");
            widgetContainer.transform.parent = transform;
            GameObject popupContainer = new GameObject("Popup");
            popupContainer.transform.parent = transform;
        }

        private void Start()
        {
            Widget.Create<Login>(true);
            Widget.Create<LoginDetail>();
            Widget.Create<Menu>();
            Widget.Create<Status>();
            Widget.Create<Blind>();
            Widget.Create<Shop>();
            Widget.Create<QuestPreparation>();
            Widget.Create<CombinationRenew>();
            
            // 모듈류.
            Widget.Create<StatusDetail>();
            Widget.Create<BattleResult>();
            Widget.Create<Inventory>();

            // 팝업류.
            Widget.Create<SelectItemCountPopup>();
            Widget.Create<CombinationResultPopup>();
            
            // 로딩창류.
            Widget.Create<LoadingScreen>();
#if DEBUG
            Widget.Create<Cheat>(true);
#endif
        }
    }
}
