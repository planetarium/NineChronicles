using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Nekoyume.UI
{
    public class DataLoadingScreen : ScreenWidget
    {
        public LoadingIndicator indicator;
        public TextMeshProUGUI toolTip;
        public Button toolTipChangeButton;

        public string Message { get; internal set; }

        private List<string> _tips;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            if (L10nManager.CurrentState == L10nManager.State.Initialized)
            {
                var message = L10nManager.Localize("BLOCK_CHAIN_MINING_TX") + "...";
                indicator.UpdateMessage(message);
                _tips = L10nManager.LocalizePattern("^UI_TIPS_[0-9]+$").Values.ToList();

                var pos = transform.localPosition;
                pos.z = -5f;
                transform.localPosition = pos;

                if (ReferenceEquals(indicator, null) ||
                    ReferenceEquals(toolTip, null))
                {
                    throw new SerializeFieldNullException();
                }
            }
            
            if (toolTipChangeButton != null)
            {
                toolTipChangeButton.onClick.AddListener(SetToolTipText);
            }
            
            L10nManager.OnLanguageChange
                .Subscribe(_ =>
                {
                    Message = L10nManager.Localize("BLOCK_CHAIN_MINING_TX") + "...";
                    _tips = L10nManager.LocalizePattern("^UI_TIPS_[0-9]+$").Values.ToList();
                })
                .AddTo(gameObject);
        }

        protected override void Update()
        {
            if (!string.IsNullOrEmpty(Message) && indicator.text.text != Message)
            {
                if (indicator.gameObject.activeSelf)
                {
                    indicator.UpdateMessage(Message);
                }
                else
                {
                    indicator.Show(Message);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            SetToolTipText();
        }

        protected override void OnDisable()
        {
            Message = L10nManager.Localize("BLOCK_CHAIN_MINING_TX") + "...";
            base.OnDisable();
        }

        public void SetToolTipText()
        {
            if (_tips != null)
            {
                toolTip.text = _tips[Random.Range(0, _tips.Count)];
            }
        }

        #endregion
    }
}
