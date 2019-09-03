using EnhancedUI.EnhancedScroller;
using Nekoyume.Helper;
using System;
using Nekoyume.Game.Mail;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class MailCellView : EnhancedScrollerCellView
    {
        public Image icon;
        public Text label;
        public Game.Mail.Mail data;
        public Button button;
        public IObservable<Unit> onClickButton;
        public IDisposable onClickDisposable;

        private Mail _mail;

        #region Mono

        private void Awake()
        {
            this.ComponentFieldsNotNullTest();
            onClickButton = button.OnClickAsObservable();
        }

        private void OnDisable()
        {
            Clear();
        }

        #endregion

        public void SetData(Game.Mail.Mail mail)
        {
            _mail = Widget.Find<Mail>();
            data = mail;
            var text = mail.ToInfo();
            Sprite sprite;
            Color32 color;
            if (mail.New)
            {
                sprite = Resources.Load<Sprite>("UI/Textures/UI_icon_quest_01");
                color = ColorHelper.HexToColorRGB("fff9dd");
                button.interactable = true;
            }
            else
            {
                sprite = Resources.Load<Sprite>("UI/Textures/UI_icon_quest_02");
                color = ColorHelper.HexToColorRGB("7a7a7a");
                button.interactable = false;
            }
            icon.sprite = sprite;
            icon.SetNativeSize();
            label.text = text;
            label.color = color;
        }

        public void Read()
        {
            if (!data.New)
                return;

            data.New = false;
            button.interactable = false;
            label.color = ColorHelper.HexToColorRGB("7a7a7a");
            data.Read(_mail);
        }

        private void Clear()
        {
            onClickDisposable?.Dispose();
            button.interactable = true;
        }
    }
}
