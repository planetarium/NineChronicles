using System;
using DG.Tweening;
using Nekoyume.Model;
using Nekoyume.State;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class Gold : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public CanvasGroup canvasGroup;
        public bool animateAlpha;

        private IDisposable _disposable;

        #region Mono

        private void OnEnable()
        {
            _disposable = ReactiveAgentState.Gold.Subscribe(SetGold);

            if (animateAlpha)
            {
                canvasGroup.alpha = 0;
                canvasGroup.DOFade(1, 1.0f);
            }
        }

        private void OnDisable()
        {
            _disposable.Dispose();
        }

        #endregion

        private void SetGold(decimal gold)
        {
            text.text = gold.ToString("n0");
        }
    }
}
