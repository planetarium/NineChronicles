using System;
using EnhancedUI.EnhancedScroller;
using Nekoyume.Model.Mail;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class NotificationCellView : EnhancedScrollerCellView
    {
        public struct Model
        {
            public MailType mailType;
            public string message;
            public string submitText;
            public System.Action submitAction;
            public DateTime addedAt;
        }

        public Action<NotificationCellView> onClickSubmitButton;

        public Button rootButton;
        public Image iconImage;
        public TextMeshProUGUI messageText;
        public Button submitButton;
        public TextMeshProUGUI submitText;

        public RectTransform RectTransform { get; private set; }
        public Model SharedModel { get; private set; }

        private void Awake()
        {
            rootButton.OnClickAsObservable()
                .Subscribe(_ => onClickSubmitButton?.Invoke(this))
                .AddTo(gameObject);
            submitButton.OnClickAsObservable()
                .Subscribe(_ => onClickSubmitButton?.Invoke(this))
                .AddTo(gameObject);
            RectTransform = GetComponent<RectTransform>();
        }

        public void SetModel(Model model)
        {
            SharedModel = model;
            iconImage.overrideSprite = Mail.mailIcons[SharedModel.mailType];
            messageText.text = model.message;

            if (string.IsNullOrEmpty(model.submitText))
            {
                submitButton.gameObject.SetActive(false);
            }
            else
            {
                submitText.text = model.submitText;
                submitButton.gameObject.SetActive(true);
            }
        }
    }
}
