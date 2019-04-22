using System;
using Nekoyume.Action;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class Gold: MonoBehaviour
    {
        public Image image;
        public TextMeshProUGUI text;

        private IDisposable _disposable;

        #region Mono

        private void OnEnable()
        {
            _disposable = Nekoyume.Model.Agent.Gold.Subscribe(SetGold);
            
            SetGold(Nekoyume.Model.Agent.Gold.Value);
        }

        private void OnDisable()
        {
            _disposable.Dispose();
        }
        
        #endregion

        private void SetGold(int gold)
        {
            text.text = gold.ToString("n0");
        }
    }
}
