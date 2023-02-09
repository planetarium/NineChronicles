namespace Lib9c.Tests.Util
{
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using Nekoyume.Model.Quest;
    using Nekoyume.TableData;

    public static class QuestUtil
    {
        public static (IAccountStateDelta, IAccountStateDelta) DisableQuestList(
            IAccountStateDelta stateV1,
            IAccountStateDelta stateV2,
            Address avatarAddress
        )
        {
            var emptyQuestList = new QuestList(
                new QuestSheet(),
                new QuestRewardSheet(),
                new QuestItemRewardSheet(),
                new EquipmentItemRecipeSheet(),
                new EquipmentItemSubRecipeSheet()
            );
            var avatarState = stateV1.GetAvatarState(avatarAddress);
            avatarState.questList = emptyQuestList;
            var newStateV1 = stateV1.SetState(avatarAddress, avatarState.Serialize());
            var newStateV2 = stateV2.SetState(
                avatarAddress.Derive(SerializeKeys.LegacyQuestListKey),
                emptyQuestList.Serialize()
            );
            return (newStateV1, newStateV2);
        }
    }
}
