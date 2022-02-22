using System;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class DimmedLoadingScreen : ScreenWidget
    {
        public override WidgetType WidgetType => WidgetType.System;

        [SerializeField]
        private TMP_Text messageText;

        private static string _defaultBlockSyncingMessage;

        protected override void Awake()
        {
            base.Awake();
            _defaultBlockSyncingMessage = L10nManager.Localize("UI_SYNCING_BLOCKS");
        }

        public void Show(string message = "", bool ignoreShowAnimation = false)
        {
            messageText.text = string.IsNullOrWhiteSpace(message) ? _defaultBlockSyncingMessage : message;
            base.Show(ignoreShowAnimation);
        }

        protected override void OnEnable()
        {
            messageText.text = _defaultBlockSyncingMessage;
            base.OnEnable();
        }
    }
}
