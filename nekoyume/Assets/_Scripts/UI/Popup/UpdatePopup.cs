using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public class UpdatePopup : SystemPopup
    {
        public override WidgetType WidgetType => WidgetType.System;

        public override void Show(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
            base.Show(title, content, labelOK, localize);
#if UNITY_EDITOR
            CloseCallback = UnityEditor.EditorApplication.ExitPlaymode;
#else
            CloseCallback = UnityEngine.Application.Quit;
#endif
        }
    }
}
