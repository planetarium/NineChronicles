using Nekoyume.Game.Controller;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public enum ConfirmResult : int
    {
        Yes,
        No,
    }

    public delegate void ConfirmDelegate(ConfirmResult result);

    public class Confirm : Popup
    {
        public Text content;
        public Text labelYes;
        public Text labelNo;
        public ConfirmDelegate CloseCallback { get; set; }
        public void Show(string text, string btnYes="YES", string btnNo="NO", bool localize=false)
        {
            if (localize)
            {
                content.text = Assets.SimpleLocalization.LocalizationManager.Localize(text);
                labelYes.text = Assets.SimpleLocalization.LocalizationManager.Localize(btnYes);
                labelNo.text = Assets.SimpleLocalization.LocalizationManager.Localize(btnNo);
            }
            else
            {
                content.text = text;
                labelYes.text = btnYes;
                labelNo.text = btnNo;
            }
            base.Show();
        }

        public void Yes()
        {
            if (CloseCallback != null)
            {
                CloseCallback(ConfirmResult.Yes);
            }
            base.Close();
            AudioController.PlayClick();
        }

        public void No()
        {
            if (CloseCallback != null)
            {
                CloseCallback(ConfirmResult.No);
            }
            base.Close();
            AudioController.PlayClick();
        }
    }
}
