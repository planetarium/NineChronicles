using Assets.SimpleLocalization;

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
            var errorMsg = string.Format(LocalizationManager.Localize("UI_ERROR_FORMAT"),
                LocalizationManager.Localize("ACTION_HANDLE"));

            base.Show(LocalizationManager.Localize("UI_ERROR"), errorMsg,
                LocalizationManager.Localize("UI_OK"), false);
            content.text += $"\n{msg}";
            base.Show();
        }
    }
}
