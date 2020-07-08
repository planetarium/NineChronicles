using System;
using Assets.SimpleLocalization;
using FancyScrollView;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class MailCell : FancyScrollRectCell<Nekoyume.Model.Mail.Mail, QuestScroll.ContextModel>
    {
        private static readonly Vector2 LeftBottom = new Vector2(-14f, -10.5f);

        private static readonly Vector2 MinusRightTop = new Vector2(14f, 13f);

        [SerializeField]
        private Image iconImage = null;

        [SerializeField]
        private TextMeshProUGUI content = null;

        [SerializeField]
        private RectTransform buttonRectTransform = null;

        [SerializeField]
        private Button button = null;

        [SerializeField]
        private TextMeshProUGUI submitText = null;

        [SerializeField]
        private TextMeshProUGUI selectedSubmitText = null;

        private Nekoyume.Model.Mail.Mail _mail;

        public Action<MailCell> onClickSubmitButton;

        private void Awake()
        {
            button.onClick
                .AsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(OnClickButton)
                .AddTo(gameObject);
        }

        public override void UpdateContent(Nekoyume.Model.Mail.Mail itemData)
        {
            _mail = itemData;
            UpdateView();
        }

        private void UpdateView()
        {
            var isNew = _mail.New;
            button.interactable = isNew;
            submitText.text = isNew
                ? LocalizationManager.Localize("UI_RECEIVE")
                : LocalizationManager.Localize("UI_RECEIVED");
            selectedSubmitText.text = submitText.text;
            buttonRectTransform.offsetMin = isNew ? LeftBottom : Vector2.zero;
            buttonRectTransform.offsetMax = isNew ? MinusRightTop : Vector2.zero;
            iconImage.overrideSprite = Mail.mailIcons[_mail.MailType];
            content.text = _mail.ToInfo();
            content.color = isNew
                ? ColorHelper.HexToColorRGB("fff9dd")
                : ColorHelper.HexToColorRGB("7a7a7a");
            submitText.gameObject.SetActive(!isNew);
            selectedSubmitText.gameObject.SetActive(isNew);
        }

        private void OnClickButton(Unit unit)
        {
            AudioController.PlayClick();

            submitText.text = LocalizationManager.Localize("UI_RECEIVED");
            buttonRectTransform.offsetMin = Vector2.zero;
            buttonRectTransform.offsetMax = Vector2.zero;
            if (!_mail.New)
            {
                return;
            }

            _mail.New = false;
            button.interactable = false;
            submitText.gameObject.SetActive(true);
            selectedSubmitText.gameObject.SetActive(false);
            content.color = ColorHelper.HexToColorRGB("7a7a7a");

            var mail = Widget.Find<Mail>();
            _mail.Read(mail);
            mail.UpdateTabs();
            onClickSubmitButton?.Invoke(this);
        }
    }
}
