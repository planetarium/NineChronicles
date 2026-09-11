using System;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.L10n
{
    using UniRx;

    [DisallowMultipleComponent][RequireComponent(typeof(TextMeshProUGUI))]
    public class L10nTextMeshProUGUI : MonoBehaviour
    {
        [SerializeField][Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 에셋을 교체할지를 설정합니다.")]
        private bool fixedFontAsset = default;

        [SerializeField][Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 스타일을 교체할지를 설정합니다.")]
        private bool fixedFontStyle = default;

        [SerializeField][Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 사이즈 오프셋을 반영할지를 설정합니다.")]
        private bool fixedFontSizeOffset = default;

        [SerializeField][Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 스페이싱 옵션을 반영할지를 설정합니다.")]
        private bool fixedSpacingOption = default;

        [SerializeField][Tooltip("`L10nManager.OnLanguageTypeSettingsChange`를 구독해서 폰트 마진 옵션을 반영할지를 설정합니다.")]
        private bool fixedMarginOption = default;

        [SerializeField]
        private FontMaterialType fontMaterialType = default;

        [SerializeField][Tooltip("L10nManager.Localize() 메소드의 인자 역할을 합니다. 값이 비어 있다면 무시합니다.")]
        private string l10nKey = null;

        private TextMeshProUGUI _textCache;

        /// <summary>
        /// 런타임에 키를 바꾼다. 프리팹에 박힌 키가 아니라 코드가 라벨을 정하는 셀
        /// (예: 시트 기준으로 구성하는 등급 필터 셀)에 쓴다.
        /// </summary>
        /// <remarks>
        /// 빈 값을 넣으면 언어 변경 때 <see cref="SetLanguage"/> 가 텍스트를 건드리지
        /// 않으므로, 아직 L10N 키가 없는 라벨을 코드가 직접 채워 넣은 상태를 지킬 수 있다.
        /// 폰트 에셋·스타일 교체는 키와 무관하게 계속 동작한다.
        /// </remarks>
        public string L10nKey
        {
            get => l10nKey;
            set
            {
                l10nKey = value;
                if (L10nManager.CurrentState == L10nManager.State.Initialized)
                {
                    SetLanguage();
                }

                // 초기화 전이면 Awake 가 등록한 OnInitialize 구독이 대신 적용한다.
                // 여기서 Localize 를 부르면 `!KEY!` 가 박힌다.
            }
        }

        [SerializeField][HideInInspector]
        private bool fontMaterialIndexInitialized = false;

        [SerializeField][HideInInspector]
        private int fontMaterialIndex;

        [SerializeField][HideInInspector]
        private bool defaultFontStylesInitialized = false;

        [SerializeField][HideInInspector]
        private FontStyles defaultFontStyles;

        [SerializeField][HideInInspector]
        private bool defaultFontSizeInitialized = false;

        [SerializeField][HideInInspector]
        private float defaultFontSize = default;

        [SerializeField][HideInInspector]
        private bool defaultCharacterSpacingInitialized = false;

        [SerializeField][HideInInspector]
        private float defaultCharacterSpacing;

        [SerializeField][HideInInspector]
        private bool defaultWordSpacingInitialized = false;

        [SerializeField][HideInInspector]
        private float defaultWordSpacing;

        [SerializeField][HideInInspector]
        private bool defaultLineSpacingInitialized = false;

        [SerializeField][HideInInspector]
        private float defaultLineSpacing;

        private IDisposable _l10nManagerOnLanguageChangeDisposable;
        private IDisposable _l10nManagerOnLanguageTypeSettingsChangeDisposable;

        private TextMeshProUGUI Text =>
            _textCache
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
