using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.L10n
{
    [DisallowMultipleComponent, RequireComponent(typeof(TextMeshProUGUI))]
    public class L10nTextMeshProUGUI : MonoBehaviour
    {
        [SerializeField,
         Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 에셋을 교체할지를 설정합니다.")]
        private bool fixedFontAsset = default;

        [SerializeField,
         Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 사이즈 오프셋을 반영할지를 설정합니다.")]
        private bool fixedFontSizeOffset = default;

        [SerializeField]
        private FontMaterialType fontMaterialType = default;

        [SerializeField, Tooltip("L10nManager.Localize() 메소드의 인자 역할을 합니다. 값이 비어 있다면 무시합니다.")]
        private string l10nKey = null;

        private TextMeshProUGUI _textCache;

        private int? _fontMaterialIndexCache;

        private float? _defaultFontSizeCache;

        private IDisposable _l10nManagerOnLanguageTypeSettingsChangeDisposable;

        private TextMeshProUGUI Text => _textCache
            ? _textCache
            : _textCache = GetComponent<TextMeshProUGUI>();

        private int FontMaterialIndex =>
            _fontMaterialIndexCache ??
            (_fontMaterialIndexCache = Text.fontMaterials
                .ToList()
                .IndexOf(Text.fontMaterial)).Value;

        private float DefaultFontSize =>
            _defaultFontSizeCache ?? (_defaultFontSizeCache = Text.fontSize).Value;

        private void Awake()
        {
            Assert.NotNull(Text);
            Assert.Greater(FontMaterialIndex, -1);
            Assert.Greater(DefaultFontSize, -1);

            if (L10nManager.CurrentState == L10nManager.State.Initialized)
            {
                SetLanguageTypeSettings(L10nManager.CurrentLanguageTypeSettings);
                SubscribeLanguageChange();
            }
            else
            {
                L10nManager.OnInitialize
                    .Subscribe(_ =>
                    {
                        SetLanguageTypeSettings(L10nManager.CurrentLanguageTypeSettings);
                        SubscribeLanguageChange();
                    })
                    .AddTo(gameObject);
            }

            if (!string.IsNullOrEmpty(l10nKey))
            {
                Text.text = L10nManager.Localize(l10nKey);
            }
        }

        private void OnDestroy()
        {
            _l10nManagerOnLanguageTypeSettingsChangeDisposable?.Dispose();
            _l10nManagerOnLanguageTypeSettingsChangeDisposable = null;
        }

        private void SubscribeLanguageChange()
        {
            _l10nManagerOnLanguageTypeSettingsChangeDisposable?.Dispose();
            _l10nManagerOnLanguageTypeSettingsChangeDisposable =
                L10nManager.OnLanguageTypeSettingsChange.Subscribe(SetLanguageTypeSettings);
        }

        private void SetLanguageTypeSettings(LanguageTypeSettings settings)
        {
            if (!fixedFontAsset)
            {
                Text.font = settings.fontAssetData.FontAsset;
                if (L10nManager.TryGetFontMaterial(fontMaterialType, out var fontMaterial))
                {
                    Text.fontSharedMaterial = fontMaterial;
                }
            }

            if (!fixedFontSizeOffset)
            {
                Text.fontSize = DefaultFontSize + settings.fontAssetData.FontSizeOffset;
            }
        }
    }
}
