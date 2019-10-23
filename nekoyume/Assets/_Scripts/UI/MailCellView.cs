using EnhancedUI.EnhancedScroller;
using Nekoyume.Helper;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.SimpleLocalization;

namespace Nekoyume.UI
{
    public class MailCellView : EnhancedScrollerCellView
    {
        public Action<MailCellView> onClickSubmitButton;

        private static readonly Color _highlightedColor = ColorHelper.HexToColorRGB("001870");
        public Game.Mail.Mail data;
        public Image icon;
        public TextMeshProUGUI content;
        public Button button;
        public Text submitText;
        public IDisposable onClickDisposable;

        private Mail _mail;
        private Shadow[] _textShadows;

        #region Mono

        private void Awake()
        {
            this.ComponentFieldsNotNullTest();
            onClickDisposable = button.OnClickAsObservable()
                .Subscribe(_ => onClickSubmitButton?.Invoke(this))
                .AddTo(gameObject);
        }

        private void OnDisable()
        {
            Clear();
        }

        #endregion

        public void SetData(Game.Mail.Mail mail)
        {   
            _textShadows = button.GetComponentsInChildren<Shadow>();
            _mail = Widget.Find<Mail>();
            data = mail;
            var text = mail.ToInfo();
            Color32 color = mail.New ? ColorHelper.HexToColorRGB("fff9dd") : ColorHelper.HexToColorRGB("7a7a7a");
            button.interactable = mail.New;
            submitText.text = LocalizationManager.Localize("UI_RECEIVE");
            foreach (var shadow in _textShadows)
                shadow.effectColor = mail.New ? _highlightedColor : Color.black;
            icon.overrideSprite = Mail.mailIcons[mail.MailType];
            icon.SetNativeSize();
            content.text = text;
            content.color = color;
        }

        public void Read()
        {
            if (!data.New)
                return;

            data.New = false;
            button.interactable = false;
            foreach (var shadow in _textShadows)
                shadow.effectColor = Color.black;
            content.color = ColorHelper.HexToColorRGB("7a7a7a");
            data.Read(_mail);
        }

        private void Clear()
        {
            onClickDisposable?.Dispose();
            button.interactable = true;
        }
    }
}
