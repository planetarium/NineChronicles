using Assets.SimpleLocalization;
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
        private enum QuestTabState
        {
            Adventure = 0,
            Obtain,
            Crafting,
            Exchange
        }

        [Serializable]
        public class TabButton
        {
            private static readonly Color HighlightedColor = ColorHelper.HexToColorRGB("a35400");
            private static readonly Vector2 LeftBottom = new Vector2(-13f, -11f);
            private static readonly Vector2 MinusRightTop = new Vector2(13f, 13f);
            private static Sprite _selectedButtonSprite;

            private static Sprite SelectedButtonSprite => _selectedButtonSprite
                ? _selectedButtonSprite
                : _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");

            public Sprite highlightedSprite;
            public Button button;
            public Image hasNotificationImage;
            public Image image;
            public Image icon;
            public TextMeshProUGUI text;
            public TextMeshProUGUI textSelected;

            public void Init(string localizationKey)
            {
                if (!button)
                {
                    throw new SerializeFieldNullException(nameof(button));
                }

                var localized = LocalizationManager.Localize(localizationKey);
                var content = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(localized.ToLower());
                text.text = content;
                textSelected.text = content;
            }

            public void ChangeColor(bool isHighlighted = false)
            {
                image.overrideSprite = isHighlighted ? SelectedButtonSprite : null;
                image.rectTransform.offsetMin = isHighlighted ? LeftBottom : Vector2.zero;
                image.rectTransform.offsetMax = isHighlighted ? MinusRightTop : Vector2.zero;
                icon.overrideSprite = isHighlighted ? highlightedSprite : null;
                text.gameObject.SetActive(!isHighlighted);
                textSelected.gameObject.SetActive(isHighlighted);
            }
        }

        [SerializeField]
        private QuestTabState tabState;

        [SerializeField]
        private QuestScroll scroll = null;

        [SerializeField]
        private TabButton[] tabButtons = null;

        [SerializeField]
        private Blur blur = null;

        private QuestList _questList;

        #region override

        public override void Initialize()
        {
            base.Initialize();

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

            for (var i = 0; i < tabButtons.Length; ++i)
            {
                tabButtons[i].ChangeColor(i == state);
            }

            var list = _questList
                .ToList()
                .FindAll(e => e.QuestType == (QuestType) state)
                .OrderBy(e => e, new QuestOrderComparer())
                .ToList();
            scroll.UpdateData(list);
        }

        public void UpdateTabs()
        {
            for (var i = 0; i < tabButtons.Length; ++i)
            {
                var cnt = _questList.Count(quest =>
                    quest.QuestType == (QuestType) i &&
                    quest.Complete &&
                    quest.isReceivable);
                tabButtons[i].hasNotificationImage.enabled = cnt > 0;
            }
        }

        public void SetList(QuestList list)
        {
            if (list is null)
            {
                return;
            }

            _questList = list;

            ChangeState((int) tabState);
        }
    }

    public class QuestOrderComparer : IComparer<QuestModel>
    {
        public int Compare(QuestModel x, QuestModel y)
        {
            // null
            if (x is null)
            {
                return y is null ? 0 : 1;
            }

            if (y is null)
            {
                return -1;
            }

            if (x.Complete && y.Complete)
            {
                if (x.isReceivable)
                {
                    if (!y.isReceivable)
                    {
                        return -1;
                    }
                }
                else
                {
                    if (y.isReceivable)
                    {
                        return 1;
                    }
                }

                return CompareId(x.Id, y.Id);
            }

            if (x.Complete)
            {
                return x.isReceivable
                    ? -1
                    : 1;
            }

            if (y.Complete)
            {
                return y.isReceivable
                    ? 1
                    : -1;
            }

            return CompareId(x.Id, y.Id);
        }

        private static int CompareId(int x, int y)
        {
            if (x > y)
            {
                return 1;
            }

            if (x == y)
            {
                return 0;
            }

            return -1;
        }
    }
}
