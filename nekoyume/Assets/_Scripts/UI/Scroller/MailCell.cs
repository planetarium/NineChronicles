using System;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class MailCell : RectCell<Nekoyume.Model.Mail.Mail, MailScroll.ContextModel>
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
        private GameObject unseal = null;

        [SerializeField]
        private TextMeshProUGUI submitText = null;

        [SerializeField]
        private TextMeshProUGUI selectedSubmitText = null;

        private Nekoyume.Model.Mail.Mail _mail;

        private void Awake()
        {
            button.onClick
                .AsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(OnClickButton)
                .AddTo(gameObject);

            SetText(ref submitText, "UI_RECEIVE", "fff9dd");
            SetText(ref selectedSubmitText, "UI_RECEIVED", "955C4A");
            buttonRectTransform.offsetMin = LeftBottom;
            buttonRectTransform.offsetMax = MinusRightTop;
        }

        public override void UpdateContent(Nekoyume.Model.Mail.Mail itemData)
        {
            _mail = itemData;
            UpdateView();
        }

        private async void UpdateView()
        {
            if (_mail is null)
            {
                Hide();
                return;
            }

            var isNew = _mail.New;

            button.gameObject.SetActive(isNew);
            unseal.SetActive(!isNew);
            iconImage.overrideSprite = SpriteHelper.GetMailIcon(_mail.MailType);

            content.text = await _mail.ToInfo();
            content.color = isNew
                ? ColorHelper.HexToColorRGB("fff9dd")
                : ColorHelper.HexToColorRGB("7a7a7a");
        }

        private void OnClickButton(Unit unit)
        {
            AudioController.PlayClick();
            button.gameObject.SetActive(false);

            if (!_mail.New)
            {
                return;
            }

            _mail.New = false;
            unseal.SetActive(true);
            content.color = ColorHelper.HexToColorRGB("7a7a7a");

            var mail = Widget.Find<MailPopup>();
            _mail.Read(mail);
            mail.UpdateTabs();
        }

        private void SetText(ref TextMeshProUGUI textMesh, string textKey, string hex)
        {
            textMesh.text = L10nManager.Localize(textKey);
            textMesh.color = ColorHelper.HexToColorRGB(hex);
        }
    }
}
