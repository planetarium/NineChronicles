using System;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.UI.Module;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class MailCell : RectCell<Nekoyume.Model.Mail.Mail, MailScroll.ContextModel>
    {
        [SerializeField]
        private Image iconImage = null;

        [SerializeField]
        private TextMeshProUGUI content = null;

        [SerializeField]
        private ConditionalButton button = null;

        private Nekoyume.Model.Mail.Mail _mail;

        private void Awake()
        {
            button.OnSubmitSubject
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(OnClickButton)
                .AddTo(gameObject);
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

            button.Interactable = isNew;
            iconImage.overrideSprite = SpriteHelper.GetMailIcon(_mail.MailType);

            content.text = await _mail.ToInfo();
            content.color = isNew
                ? ColorHelper.HexToColorRGB("fff9dd")
                : ColorHelper.HexToColorRGB("7a7a7a");
        }

        private void OnClickButton(Unit unit)
        {
            AudioController.PlayClick();
            button.Interactable = false;

            if (!_mail.New)
            {
                return;
            }

            content.color = ColorHelper.HexToColorRGB("7a7a7a");

            var mail = Widget.Find<MailPopup>();
            _mail.Read(mail);
            mail.UpdateTabs();
        }
    }
}
