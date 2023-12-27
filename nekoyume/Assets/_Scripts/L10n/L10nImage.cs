using System;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.L10n
{
    using UniRx;

    [DisallowMultipleComponent, RequireComponent(typeof(Image))]
    public class L10nImage : MonoBehaviour
    {
        [Serializable]
        private class SpritePair
        {
            public LanguageType languageType;
            public Sprite sprite;
        }

        [SerializeField]
        private SpritePair[] spritePairs;

        private Image _imageCache;
        private IDisposable _l10nManagerOnLanguageChangeDisposable;

        private Image Image => _imageCache ? _imageCache : _imageCache = GetComponent<Image>();

        private void Awake()
        {
            if (L10nManager.CurrentState == L10nManager.State.Initialized)
            {
                SetLanguage(L10nManager.CurrentLanguage);
                SubscribeLanguageChange();
            }
            else
            {
                L10nManager.OnInitialize.Subscribe(SetLanguage).AddTo(gameObject);
                SubscribeLanguageChange();
            }
        }

        private void OnDestroy()
        {
            _l10nManagerOnLanguageChangeDisposable?.Dispose();
            _l10nManagerOnLanguageChangeDisposable = null;
        }

        private void SetLanguage(LanguageType languageType)
        {
            var spritePair =
                spritePairs.FirstOrDefault(pair => pair.languageType == languageType) ??
                spritePairs.FirstOrDefault(pair => pair.languageType == LanguageType.English);

            Image.sprite = spritePair.sprite;
        }

        private void SubscribeLanguageChange()
        {
            _l10nManagerOnLanguageChangeDisposable?.Dispose();
            _l10nManagerOnLanguageChangeDisposable =
                L10nManager.OnLanguageChange.Subscribe(SetLanguage);
        }
    }
}
