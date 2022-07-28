using System;
using System.Globalization;
using Libplanet.Assets;
using Nekoyume.State;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        [SerializeField]
        private Image _image;

        public Image IconImage => _image;

        public bool NowLoading => loadingObject.activeSelf;

        public Vector3 IconPosition => iconTransform.position;

        private IDisposable _disposable;

        private int _loadingCount = 0;

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
            if (isVisible)
            {
                _loadingCount++;
            }
            else if (_loadingCount > 0)
            {
                _loadingCount--;
            }

            loadingObject.SetActive(_loadingCount > 0);
            text.enabled = _loadingCount <= 0;
        }

        private void UpdateCrystal() =>
            SetCrystal(States.Instance.CrystalBalance);

        private void SetCrystal(FungibleAssetValue crystal)
        {
            text.text = crystal.ToCurrencyNotation();
        }
    }
}
