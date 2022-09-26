using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class RuneStoneItem : MonoBehaviour
    {
        [SerializeField]
        private Image icon;

        [SerializeField]
        private TextMeshProUGUI countText;

        [SerializeField]
        private GameObject dimmed;

        [SerializeField]
        private TouchHandler touchHandler;

        [SerializeField]
        private bool showMessage = true;

        private readonly List<IDisposable> _disposables = new();

        private string _message;
        private void Awake()
        {
            touchHandler.OnClick
                .Subscribe(_ =>
                {
                    if (showMessage)
                    {
                        OneLineSystem.Push(MailType.System,
                            _message,
                            NotificationCell.NotificationType.Alert);
                    }
                })
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        public void Set(Sprite sprite, int count)
        {
            icon.sprite = sprite;
            countText.text = $"{count:#,0}";
            dimmed.SetActive(count <= 0);
            _message = count > 0
                ? L10nManager.Localize("UI_RUNE_SYSTEM_COMING_SOON")
                : L10nManager.Localize("UI_HAVE_NOT_RUNE_STONE");
        }
    }
}
