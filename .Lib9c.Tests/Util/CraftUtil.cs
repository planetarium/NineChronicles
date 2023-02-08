namespace Lib9c.Tests.Util
{
    using System.Globalization;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;

    public static class CraftUtil
    {
        public static IAccountStateDelta PrepareCombinationSlot(
            IAccountStateDelta state,
            Address avatarAddress,
            int slotIndex
        )
        {
            var slotAddress = avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                slotIndex));
            var slotState = new CombinationSlotState(
                slotAddress,
                GameConfig.RequireClearedStageLevel.CombinationEquipmentAction
            );
            state = state.SetState(slotAddress, slotState.Serialize());
            return state;
        }
    }
}
