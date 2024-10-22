using Nekoyume.Model;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using System;
using Libplanet.Crypto;
using UniRx;
using Inventory = Nekoyume.Model.Item.Inventory;

namespace Nekoyume.State
{
    /// <summary>
    /// 현재 선택된 AvatarState가 포함하는 값의 변화를 각각의 ReactiveProperty 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveAvatarState
    {
        private static readonly ReactiveProperty<Address> AddressInternal;

        public static readonly IObservable<Address> Address;

        private static readonly ReactiveProperty<Inventory> InventoryInternal;

        public static readonly IObservable<Inventory> Inventory;

        private static readonly ReactiveProperty<MailBox> MailBoxInternal;

        public static readonly IObservable<MailBox> MailBox;

        private static readonly ReactiveProperty<WorldInformation> WorldInformationInternal;

        public static readonly IObservable<WorldInformation> WorldInformation;

        private static readonly ReactiveProperty<long> ActionPointInternal;

        public static long ActionPoint => ActionPointInternal.Value;
        public static readonly IObservable<long> ObservableActionPoint;

        private static readonly ReactiveProperty<long> DailyRewardReceivedIndexInternal;

        public static long DailyRewardReceivedIndex => DailyRewardReceivedIndexInternal.Value;
        public static readonly IObservable<long> ObservableDailyRewardReceivedIndex;

        private static readonly ReactiveProperty<QuestList> QuestListInternal;

        public static QuestList QuestList => QuestListInternal.Value;
        public static readonly IObservable<QuestList> ObservableQuestList;

        private static readonly ReactiveProperty<long> RelationshipInternal;
        public static long Relationship => RelationshipInternal.Value;
        public static readonly IObservable<long> ObservableRelationship;

        static ReactiveAvatarState()
        {
            AddressInternal = new ReactiveProperty<Address>();
            InventoryInternal = new ReactiveProperty<Inventory>();
            MailBoxInternal = new ReactiveProperty<MailBox>();
            WorldInformationInternal = new ReactiveProperty<WorldInformation>();
            ActionPointInternal = new ReactiveProperty<long>();
            DailyRewardReceivedIndexInternal = new ReactiveProperty<long>();
            QuestListInternal = new ReactiveProperty<QuestList>();
            RelationshipInternal = new ReactiveProperty<long>();
            
            Address = AddressInternal.ObserveOnMainThread();
            Inventory = InventoryInternal.ObserveOnMainThread();
            MailBox = MailBoxInternal.ObserveOnMainThread();
            WorldInformation = WorldInformationInternal.ObserveOnMainThread();
            ObservableActionPoint = ActionPointInternal.ObserveOnMainThread();
            ObservableDailyRewardReceivedIndex = DailyRewardReceivedIndexInternal.ObserveOnMainThread();
            ObservableQuestList = QuestListInternal.ObserveOnMainThread();
            ObservableRelationship = RelationshipInternal.ObserveOnMainThread();
        }

        public static void Initialize(AvatarState state)
        {
            // todo: 선택된 아바타가 없을 경우 null이 들어 오는데, 이 때 아래에서 별도로 처리해줘야 하겠음.. 구독하는 쪽에서도 null 검사를 잘 하도록..
            if (state is null)
            {
                NcDebug.Log($"[{nameof(ReactiveAvatarState)}] {nameof(Initialize)}() states is null");
                return;
            }

            AddressInternal.SetValueAndForceNotify(state.address);
            InventoryInternal.SetValueAndForceNotify(state.inventory);
            MailBoxInternal.SetValueAndForceNotify(state.mailBox);
            WorldInformationInternal.SetValueAndForceNotify(state.worldInformation);
            QuestListInternal.SetValueAndForceNotify(state.questList);
        }

        public static void UpdateActionPoint(long actionPoint)
        {
            ActionPointInternal.SetValueAndForceNotify(actionPoint);
        }

        public static void UpdateInventory(Inventory inventory)
        {
            if (inventory is null)
            {
                return;
            }

            InventoryInternal.SetValueAndForceNotify(inventory);
        }

        public static void UpdateDailyRewardReceivedIndex(long index)
        {
            DailyRewardReceivedIndexInternal.SetValueAndForceNotify(index);
        }

        public static void UpdateMailBox(MailBox mailBox)
        {
            if (mailBox is null)
            {
                return;
            }

            MailBoxInternal.SetValueAndForceNotify(mailBox);
        }

        public static void UpdateQuestList(QuestList questList)
        {
            if (questList is null)
            {
                return;
            }

            QuestListInternal.SetValueAndForceNotify(questList);
        }

        public static void UpdateRelationship(long relationship)
        {
            RelationshipInternal.SetValueAndForceNotify(relationship);
        }
    }
}
