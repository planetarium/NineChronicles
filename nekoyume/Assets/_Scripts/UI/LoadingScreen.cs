using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class LoadingScreen : ScreenWidget
    {
        public Image loadingImage;
        public TextMeshProUGUI loadingText;
        public TextMeshProUGUI toolTip;

        public string Message { get; internal set; }

        private Color _color;
        private Sequence[] _sequences;
        private List<string> _tips;

        private const float AlphaToBeginning = 0.5f;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            loadingText.text = LocalizationManager.Localize("UI_IN_MINING_A_BLOCK");
            _tips = LocalizationManager.LocalizePattern("^UI_TIPS_[0-9]+$").Values.ToList();

            var pos = transform.localPosition;
            pos.z = -5f;
            transform.localPosition = pos;

            if (ReferenceEquals(loadingText, null) ||
                ReferenceEquals(loadingImage, null) ||
                ReferenceEquals(toolTip, null))
            {
                throw new SerializeFieldNullException();
            }

            _color = loadingText.color;
            _color.a = AlphaToBeginning;
        }

        protected override void Update()
        {
            if (!string.IsNullOrEmpty(Message) && loadingText?.text != Message)
            {
                loadingText.text = Message;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            toolTip.text = _tips[new System.Random().Next(0, _tips.Count)];
            loadingImage.color = _color;
            _sequences = new[]
            {
                DOTween.Sequence()
                    .Append(loadingImage.DOFade(1f, 0.3f))
                    .Append(loadingImage.DOFade(AlphaToBeginning, 0.6f))
                    .SetLoops(-1),
            };
        }

        protected override void OnDisable()
        {
            foreach (var sequence in _sequences)
            {
                sequence.Kill();
            }

            _sequences = null;
            Message = LocalizationManager.Localize("UI_IN_MINING_A_BLOCK");
            
            base.OnDisable();
        }

        #endregion
    }
}
