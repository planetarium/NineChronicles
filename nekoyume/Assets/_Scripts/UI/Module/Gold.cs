using System;
using Nekoyume.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class Gold: MonoBehaviour
    {
        public Image image;
        public Text text;

        private IDisposable _disposable;

        #region Mono

        private void OnEnable()
        {
            _disposable = ReactiveAgentState.Gold.Subscribe(SetGold);
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
