using System;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.L10n
{
    using UniRx;

    [DisallowMultipleComponent, RequireComponent(typeof(TextMeshProUGUI))]
    public class L10nTextMeshProUGUI : MonoBehaviour
    {
        [SerializeField,
         Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 에셋을 교체할지를 설정합니다.")]
        private bool fixedFontAsset = default;

        [SerializeField,
         Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 스타일을 교체할지를 설정합니다.")]
        private bool fixedFontStyle = default;

        [SerializeField,
         Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 사이즈 오프셋을 반영할지를 설정합니다.")]
        private bool fixedFontSizeOffset = default;

        [SerializeField,
         Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 스페이싱 옵션을 반영할지를 설정합니다.")]
        private bool fixedSpacingOption = default;

        [SerializeField,
        Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 마진 옵션을 반영할지를 설정합니다.")]
        private bool fixedMarginOption = default;

        [SerializeField]
        private FontMaterialType fontMaterialType = default;

        [SerializeField, Tooltip("L10nManager.Localize() 메소드의 인자 역할을 합니다. 값이 비어 있다면 무시합니다.")]
        private string l10nKey = null;

        private TextMeshProUGUI _textCache;

        [SerializeField, HideInInspector]
        private bool fontMaterialIndexInitialized = false;

        [SerializeField, HideInInspector]
        private int fontMaterialIndex;

        [SerializeField, HideInInspector]
        private bool defaultFontStylesInitialized = false;

        [SerializeField, HideInInspector]
        private FontStyles defaultFontStyles;

        [SerializeField, HideInInspector]
        private bool defaultFontSizeInitialized = false;

        [SerializeField, HideInInspector]
        private float defaultFontSize = default;

        [SerializeField, HideInInspector]
        private bool defaultCharacterSpacingInitialized = false;

        [SerializeField, HideInInspector]
        private float defaultCharacterSpacing;

        [SerializeField, HideInInspector]
        private bool defaultWordSpacingInitialized = false;

        [SerializeField, HideInInspector]
        private float defaultWordSpacing;

        [SerializeField, HideInInspector]
        private bool defaultLineSpacingInitialized = false;

        [SerializeField, HideInInspector]
        private float defaultLineSpacing;

        private IDisposable _l10nManagerOnLanguageChangeDisposable;
        private IDisposable _l10nManagerOnLanguageTypeSettingsChangeDisposable;

        private TextMeshProUGUI Text => _textCache
            ? _textCache
            : _textCache = GetComponent<TextMeshProUGUI>();

        private void Awake()
        {
            if (!fontMaterialIndexInitialized)
            {
                fontMaterialIndex = Text.fontMaterials
                    .ToList()
                    .IndexOf(Text.fontMaterial);
                fontMaterialIndexInitialized = true;
            }

            if (!defaultFontStylesInitialized)
            {
                defaultFontStyles = Text.fontStyle;
                defaultFontStylesInitialized = true;
            }

            if (!defaultFontSizeInitialized)
            {
                defaultFontSize = Text.fontSize;
                defaultFontSizeInitialized = true;
            }

            if (!defaultCharacterSpacingInitialized)
            {
                defaultCharacterSpacing = Text.characterSpacing;
                defaultCharacterSpacingInitialized = true;
            }

            if (!defaultWordSpacingInitialized)
            {
                defaultWordSpacing = Text.wordSpacing;
                defaultWordSpacingInitialized = true;
            }

            if (!defaultLineSpacingInitialized)
            {
                defaultLineSpacing = Text.lineSpacing;
                defaultWordSpacingInitialized = true;
            }

            if (L10nManager.CurrentState == L10nManager.State.Initialized)
            {
                SetLanguage();
                SetLanguageTypeSettings(L10nManager.CurrentLanguageTypeSettings);
                SubscribeLanguageChange();
            }
            else
            {
                L10nManager.OnInitialize.Subscribe(_ =>
                {
                    SetLanguage();
                    SetLanguageTypeSettings(L10nManager.CurrentLanguageTypeSettings);
                    SubscribeLanguageChange();
                }).AddTo(gameObject);
            }
        }

        private void OnDestroy()
        {
            _l10nManagerOnLanguageChangeDisposable?.Dispose();
            _l10nManagerOnLanguageChangeDisposable = null;
            _l10nManagerOnLanguageTypeSettingsChangeDisposable?.Dispose();
            _l10nManagerOnLanguageTypeSettingsChangeDisposable = null;
        }

        private void SubscribeLanguageChange()
        {
            _l10nManagerOnLanguageChangeDisposable?.Dispose();
            _l10nManagerOnLanguageChangeDisposable = 
                L10nManager.OnLanguageChange.Subscribe(_ => SetLanguage());
            _l10nManagerOnLanguageTypeSettingsChangeDisposable?.Dispose();
            _l10nManagerOnLanguageTypeSettingsChangeDisposable =
                L10nManager.OnLanguageTypeSettingsChange.Subscribe(SetLanguageTypeSettings);
        }

        private void SetLanguage()
        {
            if (!string.IsNullOrWhiteSpace(l10nKey))
            {
                Text.text = L10nManager.Localize(l10nKey);
            }
        }
        
        private void SetLanguageTypeSettings(LanguageTypeSettings settings)
        {
            var data = settings.fontAssetData;
            if (!fixedFontAsset)
            {
                Text.font = data.FontAsset;
                if (L10nManager.TryGetFontMaterial(fontMaterialType, out var fontMaterial))
                {
                    Text.fontSharedMaterial = fontMaterial;
                }
            }

            if (!fixedFontStyle)
            {
                var mask = ~FontStyles.Normal;
                if (data.SetFontStyleBoldToDisabledAsForced)
                {
                    mask &= ~FontStyles.Bold;
                }

                Text.fontStyle = defaultFontStyles & mask;
            }

            if (!fixedFontSizeOffset)
            {
                Text.fontSize = defaultFontSize + data.FontSizeOffset;
            }

            if (!fixedSpacingOption)
            {
                Text.characterSpacing = defaultCharacterSpacing + data.CharacterSpacingOffset;
                Text.wordSpacing = defaultWordSpacing + data.WordSpacingOffset;
                Text.lineSpacing = defaultLineSpacing + data.LineSpacingOffset;
            }

            if (!fixedMarginOption)
            {
                var margin = Text.margin;
                margin.w += data.MarginBottom;
                Text.margin = margin;
            }
        }
    }
}
