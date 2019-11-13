using Assets.SimpleLocalization;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ActionFailPopup : SystemPopup
    {
        public void Show(string msg)
        {
            var errorMsg = string.Format(LocalizationManager.Localize("UI_ERROR_FORMAT"),
                LocalizationManager.Localize("ACTION_HANDLE"));

            base.Show(LocalizationManager.Localize("UI_ERROR"), errorMsg,
                LocalizationManager.Localize("UI_OK"), false);
#if UNITY_EDITOR
            CloseCallback = UnityEditor.EditorApplication.ExitPlaymode;
#else
            CloseCallback = Application.Quit;
#endif
            content.text += $"\n{msg}";
            base.Show();
        }
    }
}
