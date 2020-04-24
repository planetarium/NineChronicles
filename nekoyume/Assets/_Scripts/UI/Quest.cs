using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Helper;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI
{
    public class Quest : XTweenWidget
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
            private static readonly Vector2 _leftBottom = new Vector2(-13f, -11f);
            private static readonly Vector2 _minusRightTop = new Vector2(13f, 13f);
            public Sprite highlightedSprite;
            public Button button;
            public Image hasNotificationImage;
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
        public TabButton[] tabButtons;
        public Blur blur;

        private static Sprite _selectedButtonSprite;
        private QuestList _questList;

        #region override

        public override void Initialize()
        {
            base.Initialize();
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");

            tabButtons[0].Init("ADVENTURE");
            tabButtons[1].Init("OBTAIN");
            tabButtons[2].Init("CRAFT");
            tabButtons[3].Init("EXCHANGE");
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            tabState = QuestTabState.Adventure;
            _questList = States.Instance.CurrentAvatarState.questList;
            ChangeState(0);
            UpdateTabs();
            base.Show(ignoreShowAnimation);

            if (blur)
            {
                blur.Show();
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (blur)
            {
                blur.Close();
            }
            
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        public void ChangeState(int state)
        {
            tabState = (QuestTabState) state;

            for (int i = 0; i < tabButtons.Length; ++i)
            {
                tabButtons[i].ChangeColor(i == state);
            }

            var list = _questList.ToList();
            list = list.FindAll(e => e.QuestType == (QuestType) state)
                .OrderBy(e => e, new QuestOrderComparer())
                .ToList();

            scroller.SetData(list);
        }

        public void UpdateTabs()
        {
            for (int i = 0; i < tabButtons.Length; ++i)
            {
                int cnt = _questList.Where(quest => quest.QuestType == (QuestType) i && quest.Complete && quest.isReceivable).Count();
                tabButtons[i].hasNotificationImage.enabled = cnt > 0;
            }
        }

        public void SetList(QuestList list)
        {
            if (list is null)
                return;
            _questList = list;

            float pos = scroller.scroller.ScrollPosition;
            ChangeState((int) tabState);
            scroller.scroller.ScrollPosition = pos;
        }
    }

    public class QuestOrderComparer : IComparer<QuestModel>
    {
        public int Compare(QuestModel x, QuestModel y)
        {
            // null
            if (x is null)
                return y is null ? 0 : 1;

            if (y is null)
                return -1;

            if(x.Complete && y.Complete)
            {
                if(x.isReceivable)
                {
                    if (!y.isReceivable)
                        return -1;
                }
                else
                {
                    if (y.isReceivable)
                        return 1;
                }

                return CompareId(x.Id, y.Id);
            }

            if (x.Complete)
            {
                if (x.isReceivable)
                    return -1;
                else
                    return 1;
            }

            if (y.Complete)
            {
                if (y.isReceivable)
                    return 1;
                else
                    return -1;
            }

            return CompareId(x.Id, y.Id);
        }

        private int CompareId(int x, int y)
        {
            if (x > y)
                return 1;

            if (x == y)
                return 0;

            return -1;
        }
    }
}
