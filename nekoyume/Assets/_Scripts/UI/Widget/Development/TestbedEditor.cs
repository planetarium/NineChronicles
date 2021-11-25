using Nekoyume.EnumType;
using Nekoyume.UI;
using UniRx;
using UnityEngine;

namespace Nekoyume
{
    public class TestbedEditor : Widget
    {
        public override WidgetType WidgetType => WidgetType.Development;

        [SerializeField]
        private GameObject content;

        public void OnClickActive()
        {
            content.SetActive(!content.activeSelf);
        }

        public void OnClickStuffTheShop()
        {
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            Game.Game.instance.ActionManager.CreateTestbed().Subscribe();
#endif
        }
    }
}
