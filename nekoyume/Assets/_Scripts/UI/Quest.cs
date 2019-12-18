using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game.Quest;
using Nekoyume.Helper;
using Nekoyume.UI.Scroller;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
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
            private static readonly Color _highlightedColor = ColorHelper.HexToColorRGB("a35400");
            private static readonly Vector2 _leftBottom = new Vector2(-15f, -10.5f);
            private static readonly Vector2 _minusRightTop = new Vector2(15f, 13f);
            public Sprite highlightedSprite;
            public Button button;
            public Image image;
            public Image icon;
            public TextMeshProUGUI text;
            public TextMeshProUGUI textSelected;

            public void Init(string localizationKey)
            {
                if (!button) return;
                var localized = LocalizationManager.Localize(localizationKey);
                var content = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(localized.ToLower());
                text.text = content;
                textSelected.text = content;
            }

            public void ChangeColor(bool isHighlighted = false)
            {
                image.overrideSprite = isHighlighted ? _selectedButtonSprite : null;
                image.rectTransform.offsetMin = isHighlighted ? _leftBottom : Vector2.zero;
                image.rectTransform.offsetMax = isHighlighted ? _minusRightTop : Vector2.zero;
                icon.overrideSprite = isHighlighted ? highlightedSprite : null;
                text.gameObject.SetActive(!isHighlighted);
                textSelected.gameObject.SetActive(isHighlighted);
            }
        }

        public QuestTabState tabState;
        public QuestScrollerController scroller;
        public TabButton adventureButton;
        public TabButton obtainButton;
        public TabButton craftingButton;
        public TabButton exchangeButton;
        public Blur blur;

        private static Sprite _selectedButtonSprite;
        private QuestList _questList;

        #region override

        public override void Initialize()
        {
            base.Initialize();
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");

            adventureButton.Init("ADVENTURE");
            obtainButton.Init("OBTAIN");
            craftingButton.Init("CRAFT");
            exchangeButton.Init("EXCHANGE");
        }

        public override void Show()
        {
            tabState = QuestTabState.Adventure;
            _questList = States.Instance.CurrentAvatarState.Value.questList;
            ChangeState(0);
            base.Show();
            blur?.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            blur?.Close();
            base.Close(ignoreCloseAnimation);
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
            list = list.FindAll(e => e.QuestType == (QuestType) state)
                .OrderBy(e => e, new QuestOrderComparer())
                .ToList();

            scroller.SetData(list);
        }
    }

    public class QuestOrderComparer : IComparer<Game.Quest.Quest>
    {
        public int Compare(Game.Quest.Quest x, Game.Quest.Quest y)
        {
            // null
            if (x is null)
                return y is null ? 0 : 1;

            if (y is null)
                return -1;
            
            // receive
            if (x.Receive)
            {
                if (!y.Receive)
                    return 1;
                
                if (x.Id > y.Id)
                    return 1;
                    
                if (x.Id == y.Id)
                    return 0;
                    
                return -1;
            }

            if (y.Receive)
                return -1;
            
            // complete
            if (x.Complete)
                return y.Complete ? 0 : -1;
            
            return y.Complete ? 1 : 0;
        }
    }
}
