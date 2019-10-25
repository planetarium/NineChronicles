using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game.Quest;
using Nekoyume.Helper;
using Nekoyume.UI.Scroller;
using System;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Quest : Widget
    {
        public enum QuestTabState
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
            public QuestTabState state;
            private Shadow[] _textShadows;

            public void Init(QuestTabState state, string localizationKey)
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

        public QuestTabState tabState;
        public QuestScrollerController scroller;
        public TabButton adventureButton;
        public TabButton obtainButton;
        public TabButton craftingButton;
        public TabButton exchangeButton;

        private static Sprite _selectedButtonSprite;
        private QuestList _questList;

        #region override

        public override void Initialize()
        {
            base.Initialize();
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");

            adventureButton.Init(QuestTabState.Adventure, "ADVENTURE");
            obtainButton.Init(QuestTabState.Obtain, "OBTAIN");
            craftingButton.Init(QuestTabState.Crafting, "CRAFT");
            exchangeButton.Init(QuestTabState.Exchange, "EXCHANGE");
        }

        public override void Show()
        {
            tabState = QuestTabState.Adventure;
            _questList = States.Instance.CurrentAvatarState.Value.questList;
            ChangeState(0);
            base.Show();
        }

        #endregion

        public void ChangeState(int state)
        {
            tabState = (QuestTabState) state;
            adventureButton.ChangeColor(tabState == QuestTabState.Adventure);
            obtainButton.ChangeColor(tabState == QuestTabState.Obtain);
            craftingButton.ChangeColor(tabState == QuestTabState.Crafting);
            exchangeButton.ChangeColor(tabState == QuestTabState.Exchange);

            var list = _questList.ToList();
            list = list.FindAll(quest => quest.QuestType == (QuestType) state);

            scroller.SetData(list);
        }
    }
}
