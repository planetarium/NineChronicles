using Nekoyume.Model;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using System;
using Libplanet.Crypto;
using UniRx;
using UnityEngine;
using Inventory = Nekoyume.Model.Item.Inventory;

namespace Nekoyume.State
{
    /// <summary>
    /// 현재 선택된 AvatarState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveAvatarState
    {
        private static readonly ReactiveProperty<Address> _address
            = new ReactiveProperty<Address>();

        public static IObservable<Address> Address => _address.ObserveOnMainThread();

        private static readonly ReactiveProperty<Inventory> _inventory
            = new ReactiveProperty<Inventory>();

        public static IObservable<Inventory> Inventory => _inventory.ObserveOnMainThread();

        private static readonly ReactiveProperty<MailBox> _mailBox
            = new ReactiveProperty<MailBox>();

        public static IObservable<MailBox> MailBox => _mailBox.ObserveOnMainThread();

        private static readonly ReactiveProperty<WorldInformation> _worldInformation
            = new ReactiveProperty<WorldInformation>();

        public static IObservable<WorldInformation> WorldInformation
            => _worldInformation.ObserveOnMainThread();

        private static readonly ReactiveProperty<long> _actionPoint = new();

        public static long ActionPoint => _actionPoint.Value;
        public static IObservable<long> ObservableActionPoint => _actionPoint.ObserveOnMainThread();

        private static readonly ReactiveProperty<long> _dailyRewardReceivedIndex = new();

        public static long DailyRewardReceivedIndex => _dailyRewardReceivedIndex.Value;
        public static IObservable<long> ObservableDailyRewardReceivedIndex
            => _dailyRewardReceivedIndex.ObserveOnMainThread();

        private static readonly ReactiveProperty<QuestList> _questList
            = new ReactiveProperty<QuestList>();

        public static IObservable<QuestList> QuestList => _questList.ObserveOnMainThread();

        public static void Initialize(AvatarState state)
        {
            // todo: 선택된 아바타가 없을 경우 null이 들어 오는데, 이 때 아래에서 별도로 처리해줘야 하겠음.. 구독하는 쪽에서도 null 검사를 잘 하도록..
            if (state is null)
            {
                NcDebug.Log($"[{nameof(ReactiveAvatarState)}] {nameof(Initialize)}() states is null");
                return;
            }

            _address.SetValueAndForceNotify(state.address);
            _inventory.SetValueAndForceNotify(state.inventory);
            _mailBox.SetValueAndForceNotify(state.mailBox);
            _worldInformation.SetValueAndForceNotify(state.worldInformation);
            _questList.SetValueAndForceNotify(state.questList);
        }

        public static void UpdateActionPoint(long actionPoint)
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
