using Nekoyume.Game.Mail;
using Nekoyume.Game.Quest;
using Nekoyume.State;
using UniRx;
using Inventory = Nekoyume.Game.Item.Inventory;

namespace Nekoyume.Model
{
    /// <summary>
    /// 현재 선택된 AvatarState가 포함하는 값의 변화를 각각의 ReactiveProperty<T> 필드를 통해 외부에 변화를 알린다.
    /// </summary>
    public static class ReactiveCurrentAvatarState
    {
        public static readonly ReactiveProperty<Inventory> Inventory = new ReactiveProperty<Inventory>();
        public static readonly ReactiveProperty<MailBox> MailBox = new ReactiveProperty<MailBox>();
        public static readonly ReactiveProperty<int> WorldStage = new ReactiveProperty<int>();
        public static readonly ReactiveProperty<int> ActionPoint = new ReactiveProperty<int>();
        public static readonly ReactiveProperty<long> DailyRewardReceivedIndex = new ReactiveProperty<long>();
        public static readonly ReactiveProperty<QuestList> QuestList = new ReactiveProperty<QuestList>();

        public static void Initialize(AvatarState avatarState)
        {
            if (ReferenceEquals(avatarState, null))
            {
                return;
            }
            
            Inventory.SetValueAndForceNotify(avatarState.inventory);
            MailBox.SetValueAndForceNotify(avatarState.mailBox);
            WorldStage.SetValueAndForceNotify(avatarState.worldStage);
            ActionPoint.SetValueAndForceNotify(avatarState.actionPoint);
            DailyRewardReceivedIndex.SetValueAndForceNotify(avatarState.dailyRewardReceivedIndex);
            QuestList.SetValueAndForceNotify(avatarState.questList);
        }
    }
}
