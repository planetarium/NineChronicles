using Nekoyume.Action;
using Nekoyume.L10n;

namespace Nekoyume.UI
{
    public class ActionFailPopup : SystemPopup
    {
        protected override void Awake()
        {
            base.Awake();

            SubmitWidget = () => Close();
        }

        public void Show(string msg)
        {
            var errorMsg = string.Format(L10nManager.Localize("UI_ERROR_FORMAT"),
                L10nManager.Localize("ACTION_HANDLE"));

            base.Show(L10nManager.Localize("UI_ERROR"), errorMsg,
                L10nManager.Localize("UI_OK"), false);
            content.text += $"\n{msg}";
            base.Show();
        }

        public void Show<T>(string msg) where T : ActionBase
        {
            Show($"[{typeof(T).Name}] {msg}");
        }
    }
}
