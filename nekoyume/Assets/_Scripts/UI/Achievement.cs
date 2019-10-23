using Assets.SimpleLocalization;
using Nekoyume.Helper;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Achievement : Widget
    {
        public enum AchievementTabState
        {
            Adventure = 0,
            Obtain,
            Crafting,
            Exchange
        }

        [Serializable]
        public class TabButton
        {
            private static readonly Color _highlightedColor = ColorHelper.HexToColorRGB("001870");
            private static readonly Vector2 _highlightedSize = new Vector2(143f, 60f);
            private static readonly Vector2 _unHighlightedSize = new Vector2(116f, 36f);
            public Sprite highlightedSprite;
            public Button button;
            public Image image;
            public Image icon;
            public Text text;
            public AchievementTabState state;
            private Shadow[] _textShadows;

            public void Init(AchievementTabState state, string localizationKey)
            {
                if (!button) return;
                _textShadows = button.GetComponentsInChildren<Shadow>();
                var localized = LocalizationManager.Localize(localizationKey);
                var content = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(localized.ToLower());
                text.text = content;
            }

            public void ChangeColor(bool isHighlighted = false)
            {
                image.overrideSprite = isHighlighted ? _selectedButtonSprite : null;
                // 금색 버튼 리소스로 변경 시 주석 해제
                // image.rectTransform.sizeDelta = isHighlighted ? _highlightedSize : _unHighlightedSize;
                icon.overrideSprite = isHighlighted ? highlightedSprite : null;
                foreach (var shadow in _textShadows)
                    shadow.effectColor = isHighlighted ? _highlightedColor : Color.black;
            }
        }

        public AchievementTabState tabState;
        public TabButton adventureButton;
        public TabButton obtainButton;
        public TabButton craftingButton;
        public TabButton exchangeButton;

        private static Sprite _selectedButtonSprite;

        #region override

        public override void Initialize()
        {
            base.Initialize();
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");

            adventureButton.Init(AchievementTabState.Adventure, "ADVENTURE");
            obtainButton.Init(AchievementTabState.Obtain, "OBTAIN");
            craftingButton.Init(AchievementTabState.Crafting, "CRAFT");
            exchangeButton.Init(AchievementTabState.Exchange, "EXCHANGE");
        }

        public override void Show()
        {
            tabState = AchievementTabState.Adventure;
            ChangeState(0);
            base.Show();
        }

        #endregion

        public void ChangeState(int state)
        {
            tabState = (AchievementTabState)state;
            adventureButton.ChangeColor(tabState == AchievementTabState.Adventure);
            obtainButton.ChangeColor(tabState == AchievementTabState.Obtain);
            craftingButton.ChangeColor(tabState == AchievementTabState.Crafting);
            exchangeButton.ChangeColor(tabState == AchievementTabState.Exchange);

        }
    }
}
