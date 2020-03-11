using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    public class AvatarDailyRewardReceivedIndexModifier : AvatarStateModifier
    {
        [SerializeField]
        private int blockIndex;

        public override bool IsEmpty => blockIndex == 0;

        public AvatarDailyRewardReceivedIndexModifier(int blockIndex)
        {
            this.blockIndex = blockIndex;
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarDailyRewardReceivedIndexModifier m))
                return;

            blockIndex += m.blockIndex;
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarDailyRewardReceivedIndexModifier m))
                return;

            blockIndex -= m.blockIndex;
        }

        public override AvatarState Modify(AvatarState state)
        {
            if (state is null)
                return null;

            state.dailyRewardReceivedIndex += blockIndex;
            return state;
        }

        public override string ToString()
        {
            return $"[{nameof(AvatarDailyRewardReceivedIndexModifier)}] {nameof(blockIndex)}: {blockIndex}";
        }
    }
}
