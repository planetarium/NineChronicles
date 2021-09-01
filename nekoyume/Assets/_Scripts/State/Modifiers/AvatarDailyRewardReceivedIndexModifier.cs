using System;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarDailyRewardReceivedIndexModifier : AvatarStateModifier
    {
        [SerializeField]
        private long blockCount;

        public override bool IsEmpty => blockCount == 0;

        public AvatarDailyRewardReceivedIndexModifier(long blockCount)
        {
            this.blockCount = blockCount;
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarDailyRewardReceivedIndexModifier m))
            {
                return;
            }

            blockCount += m.blockCount;
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarDailyRewardReceivedIndexModifier m))
            {
                return;
            }

            blockCount -= m.blockCount;
        }

        public override AvatarState Modify(AvatarState state)
        {
            return state;

            // if (state is null)
            // {
            //     return null;
            // }
            //
            // state.dailyRewardReceivedIndex += blockCount;
            // return state;
        }

        public override string ToString()
        {
            return $"[{nameof(AvatarDailyRewardReceivedIndexModifier)}] {nameof(blockCount)}: {blockCount}";
        }
    }
}
