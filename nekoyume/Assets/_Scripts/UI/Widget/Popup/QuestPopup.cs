using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using UnityEngine;
using UnityEngine.UI;
using QuestModel = Nekoyume.Model.Quest.Quest;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    using UniRx;

    public class QuestPopup : PopupWidget
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
        private Button closeButton = null;

        [SerializeField]
        private Button receiveAllButton;

        [SerializeField]
        private GameObject receiveAllContainer;

        private ReactiveProperty<QuestList> _questList = new ReactiveProperty<QuestList>();

        private readonly Module.ToggleGroup _toggleGroup = new Module.ToggleGroup();

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
            receiveAllButton.onClick.AddListener(ReceiveAll);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            _questList.SetValueAndForceNotify(States.Instance.CurrentAvatarState.questList);
            _toggleGroup.SetToggledOffAll();
            adventureButton.SetToggledOn();
            ChangeState(0);
            DoneScrollAnimation();
            base.Show(ignoreShowAnimation);
        }

        #endregion

        public void ChangeState(int state)
        {
            filterType = (QuestType)state;

            var list = _questList.Value
                .ToList()
                .FindAll(e => e.QuestType == (QuestType)state)
                .Where(quest => TableSheets.Instance.QuestSheet.ContainsKey(quest.Id))
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

            _questList.SetValueAndForceNotify(list);
            ChangeState((int)filterType);
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

            foreach (var questType in (QuestType[])Enum.GetValues(typeof(QuestType)))
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

            receiveAllContainer.SetActive(list.Any(quest =>
                quest.IsPaidInAction && quest.isReceivable));
        }

        private void ReceiveAll()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var questList = _questList.Value;
            var mailRewards = questList
                .Where(q => q.isReceivable && q.Complete)
                .SelectMany(q =>
                {
                    LocalLayerModifier.RemoveReceivableQuest(avatarAddress, q.Id, false);
                    // 퀘스트 받음 처리
                    q.isReceivable = false;
                    return q.Reward.ItemMap;
                }).Select(itemMap =>
                {
                    var item = ItemFactory.CreateMaterial(
                        TableSheets.Instance.MaterialItemSheet,
                        itemMap.Item1);
                    var itemId = item.ItemId;
                    var count = itemMap.Item2;
                    LocalLayerModifier.AddItem(avatarAddress, itemId, count, false);
                    return new MailReward(item, count);
                }).ToList();
            // 퀘스트 완료처리된 목록으로 갱신해서 레드닷 비활성화처리
            ReactiveAvatarState.UpdateQuestList(questList);
            _questList.SetValueAndForceNotify(questList);
            Find<MailRewardScreen>().Show(mailRewards);
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
