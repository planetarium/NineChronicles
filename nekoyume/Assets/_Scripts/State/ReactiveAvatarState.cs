using Libplanet;
using Nekoyume.Model;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using System;
using UniRx;
using Inventory = Nekoyume.Model.Item.Inventory;

namespace Nekoyume.State
{
    /// <summary>
    /// 현재 선택된 AvatarState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveAvatarState
    {
        private static readonly ReactiveProperty<Address> _address;
        public static IObservable<Address> Address;

        private static readonly ReactiveProperty<Inventory> _inventory;
        public static IObservable<Inventory> Inventory;

        private static readonly ReactiveProperty<MailBox> _mailBox;
        public static readonly IObservable<MailBox> MailBox;

        private static readonly ReactiveProperty<WorldInformation> _worldInformation;
        public static readonly IObservable<WorldInformation> WorldInformation;

        private static readonly ReactiveProperty<int> _actionPoint;
        public static readonly IObservable<int> ActionPoint;

        private static readonly ReactiveProperty<long> _dailyRewardReceivedIndex;
        public static readonly IObservable<long> DailyRewardReceivedIndex;

        private static readonly ReactiveProperty<QuestList> _questList;
        public static readonly IObservable<QuestList> QuestList;

        static ReactiveAvatarState()
        {
            _address = new ReactiveProperty<Address>();
            Address = _address.ObserveOnMainThread();

            _inventory = new ReactiveProperty<Inventory>();
            Inventory = _inventory.ObserveOnMainThread();

            _mailBox = new ReactiveProperty<MailBox>();
            MailBox = _mailBox.ObserveOnMainThread();

            _worldInformation = new ReactiveProperty<WorldInformation>();
            WorldInformation = _worldInformation.ObserveOnMainThread();

            _actionPoint = new ReactiveProperty<int>();
            ActionPoint = _actionPoint.ObserveOnMainThread();

            _dailyRewardReceivedIndex = new ReactiveProperty<long>();
            DailyRewardReceivedIndex = _dailyRewardReceivedIndex.ObserveOnMainThread();

            _questList = new ReactiveProperty<QuestList>();
            QuestList = _questList.ObserveOnMainThread();
        }

        public static void Initialize(AvatarState state)
        {
            // todo: 선택된 아바타가 없을 경우 null이 들어 오는데, 이 때 아래에서 별도로 처리해줘야 하겠음.. 구독하는 쪽에서도 null 검사를 잘 하도록..
            if (state is null)
            {
                return;
            }

            _address.SetValueAndForceNotify(state.address);
            _inventory.SetValueAndForceNotify(state.inventory);
            _mailBox.SetValueAndForceNotify(state.mailBox);
            _worldInformation.SetValueAndForceNotify(state.worldInformation);
            _actionPoint.SetValueAndForceNotify(state.actionPoint);
            _dailyRewardReceivedIndex.SetValueAndForceNotify(state.dailyRewardReceivedIndex);
            _questList.SetValueAndForceNotify(state.questList);
        }

        public static void UpdateActionPoint(int actionPoint)
        {
            _actionPoint.SetValueAndForceNotify(actionPoint);
        }

        public static void UpdateInventory(Inventory inventory)
        {
            if (inventory is null)
            {
                return;
            }

            _inventory.SetValueAndForceNotify(inventory);
        }

        public static void UpdateDailyRewardReceivedIndex(long index)
        {
            _dailyRewardReceivedIndex.SetValueAndForceNotify(index);
        }

        public static void UpdateMailBox(MailBox mailBox)
        {
            if (mailBox is null)
            {
                return;
            }

            _mailBox.SetValueAndForceNotify(mailBox);
        }

        public static void UpdateQuestList(QuestList questList)
        {
            if (questList is null)
            {
                return;
            }

            _questList.SetValueAndForceNotify(questList);
        }
    }
}
