using EnhancedUI.EnhancedScroller;
using Nekoyume.Helper;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.SimpleLocalization;

namespace Nekoyume.UI.Scroller
{
    public class MailCellView : EnhancedScrollerCellView
    {
        public Action<MailCellView> onClickSubmitButton;

        private static readonly Vector2 _leftBottom = new Vector2(-14f, -10.5f);
        private static readonly Vector2 _minusRightTop = new Vector2(14f, 13f);
        private static readonly Color _highlightedColor = ColorHelper.HexToColorRGB("a35400");
        public Game.Mail.Mail data;
        public Image icon;
        public Image buttonImage;
        public TextMeshProUGUI content;
        public Button button;
        public Text submitText;
        public IDisposable onClickDisposable;

        private Mail _mail;
        private Shadow[] _textShadows;

        #region Mono

        private void Awake()
        {
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
            buttonImage.rectTransform.offsetMin = mail.New ? _leftBottom : Vector2.zero;
            buttonImage.rectTransform.offsetMax = mail.New ? _minusRightTop : Vector2.zero;
            icon.overrideSprite = Mail.mailIcons[mail.MailType];
            content.text = text;
            content.color = color;
        }

        public void Read()
        {
            submitText.text = LocalizationManager.Localize("UI_RECEIVED");
            buttonImage.rectTransform.offsetMin = Vector2.zero;
            buttonImage.rectTransform.offsetMax = Vector2.zero;
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
