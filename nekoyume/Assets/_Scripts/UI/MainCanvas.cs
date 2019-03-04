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
            Widget.Create<Status>();
            Widget.Create<Inventory>();
            Widget.Create<SkillController>();
            Widget.Create<Menu>();
            Widget.Create<Blind>();
            Widget.Create<LoginDetail>();
            Widget.Create<StatusDetail>();
            Widget.Create<Shop>();
            Widget.Create<BattleResult>();
            Widget.Create<QuestPreparation>();
#if DEBUG
            Widget.Create<Cheat>(true);
#endif
        }
    }
}
