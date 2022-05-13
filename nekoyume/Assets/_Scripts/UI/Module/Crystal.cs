using System;
using Libplanet.Assets;
using Nekoyume.State;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using Nekoyume.State.Subjects;
    using UniRx;

    public class Crystal : AlphaAnimateModule
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private GameObject loadingObject;

        [SerializeField]
        private Transform iconTransform;

        public bool NowCharging => loadingObject.activeSelf;

        public Vector3 IconPosition => iconTransform.position;

        private IDisposable _disposable;

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposable = AgentStateSubject.Crystal.Subscribe(SetCrystal);
            UpdateCrystal();
        }

        protected override void OnDisable()
        {
            _disposable.Dispose();
            base.OnDisable();
        }

        public void SetProgressCircle(bool isVisible)
        {
            loadingObject.SetActive(isVisible);
            text.enabled = !isVisible;
        }

        private void UpdateCrystal()
        {
            if (AgentStateSubject.Crystal is null)
            {
                return;
            }

            SetCrystal(States.Instance.CrystalBalance);
        }

        private void SetCrystal(FungibleAssetValue crystal)
        {
            text.text = crystal.GetQuantityString();
        }
    }
}
