using Nekoyume.EnumType;
using UnityEngine;

namespace Nekoyume.UI
{
    public class SystemPopup : Alert
    {
        protected override WidgetType WidgetType => WidgetType.SystemInfo;

        public override void Show(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
            base.Show(title, content, labelOK, localize);
#if UNITY_EDITOR
            CloseCallback = UnityEditor.EditorApplication.ExitPlaymode;
#else
            CloseCallback = Application.Quit;
#endif
        }
    }
}
