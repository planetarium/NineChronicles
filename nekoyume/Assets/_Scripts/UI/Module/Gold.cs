using System;
using System.Globalization;
using System.Numerics;
using Nekoyume.State;
using Nekoyume.UI.Module.Common;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class Gold : AlphaAnimateModule
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        private IDisposable _disposable;

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposable = ReactiveAgentState.Gold.Subscribe(SetGold);
        }

        protected override void OnDisable()
        {
            _disposable.Dispose();
            base.OnDisable();
        }

        private void SetGold(BigInteger gold)
        {
            text.text = gold.ToString("n0", CultureInfo.InvariantCulture);
        }
    }
}
