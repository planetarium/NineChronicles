using Nekoyume.Helper;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QuestModel = Nekoyume.Model.Quest.Quest;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    using UniRx;
    public class Quest : XTweenWidget
    {
        [SerializeField]
        private CategoryTabButton adventureButton = null;

        [SerializeField]
        private CategoryTabButton obtainButton = null;

        [SerializeField]
        private CategoryTabButton craftingButton = null;

        [SerializeField]
        private CategoryTabButton exchangeButton = null;

        [SerializeField]
        private QuestType filterType;

        [SerializeField]
        private QuestScroll scroll = null;

        [SerializeField]
        private Blur blur = null;

        [SerializeField]
        private Button closeButton = null;

        private ReactiveProperty<QuestList> _questList = new ReactiveProperty<QuestList>();

        private readonly Module.ToggleGroup _toggleGroup = new Module.ToggleGroup();

        public override WidgetType WidgetType => WidgetType.Popup;
        public override CloseKeyType CloseKeyType => CloseKeyType.Escape;

        #region override
        protected override void Awake()
        {
            base.Awake();
            _toggleGroup.RegisterToggleable(adventureButton);
            _toggleGroup.RegisterToggleable(obtainButton);
            _toggleGroup.RegisterToggleable(craftingButton);
            _toggleGroup.RegisterToggleable(exchangeButton);
            _questList.Subscribe(OnQuestListChanged);
            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            _questList.Value = States.Instance.CurrentAvatarState.questList;
            _toggleGroup.SetToggledOffAll();
            adventureButton.SetToggledOn();
            ChangeState(0);
            DoneScrollAnimation();
            base.Show(ignoreShowAnimation);

            if (blur)
            {
                blur.Show();
            }
            HelpPopup.HelpMe(100011, true);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (blur && blur.isActiveAndEnabled)
            {
                blur.Close();
            }

            base.Close(ignoreCloseAnimation);
        }

        #endregion

        public void ChangeState(int state)
        {
            filterType = (QuestType) state;

            var list = _questList.Value.ToList()
                .FindAll(e => e.QuestType == (QuestType) state)
                .OrderBy(e => e, new QuestOrderComparer())
                .ToList();
            scroll.UpdateData(list, true);
        }

        public void DoneScrollAnimation()
        {
            scroll.DoneAnimation();
        }

        public void SetList(QuestList list)
        {
            if (list is null)
            {
                return;
            }

            _questList.Value = list;
            ChangeState((int) filterType);
        }

        public void EnqueueCompletedQuest(QuestModel quest)
        {
            scroll.EnqueueCompletedQuest(quest);
        }

        public void DisappearAnimation(int index)
        {
            scroll.DisappearAnimation(index);
        }

        private void OnQuestListChanged(QuestList list)
        {
            if (list is null)
            {
                return;
            }

            foreach (var questType in (QuestType[]) Enum.GetValues(typeof(QuestType)))
            {
                var button = questType switch
                {
                    QuestType.Adventure => adventureButton,
                    QuestType.Obtain => obtainButton,
                    QuestType.Craft => craftingButton,
                    QuestType.Exchange => exchangeButton
                };
                button.HasNotification.Value = list.Any(quest =>
                    quest.QuestType == questType &&
                    quest.Complete &&
                    quest.isReceivable);
            }
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
