using System;
using Libplanet.Assets;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class Crystal : AlphaAnimateModule
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        private IDisposable _disposable;

        [SerializeField]
        private GameObject loadingObject;

        public bool NowCharging => loadingObject.activeSelf;

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposable = ReactiveCrystalState.Crystal.Subscribe(SetCrystal);
            UpdateCrystal();
        }

        protected override void OnDisable()
        {
            _disposable.Dispose();
            base.OnDisable();
        }

        private void UpdateCrystal()
        {
            if (ReactiveCrystalState.Crystal is null)
            {
                return;
            }

            SetCrystal(ReactiveCrystalState.CrystalBalance);
        }

        private void SetCrystal(FungibleAssetValue crystal)
        {
            text.text = crystal.GetQuantityString();
        }

        private void SetProgressCircle(bool isVisible)
        {
            loadingObject.SetActive(isVisible);
            text.enabled = !isVisible;
        }
    }
}
