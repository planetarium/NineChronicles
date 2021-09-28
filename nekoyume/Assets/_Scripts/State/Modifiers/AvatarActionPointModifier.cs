using System;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarActionPointModifier : AvatarStateModifier
    {
        [SerializeField]
        private int actionPoint;

        public override bool IsEmpty => actionPoint == 0;

        public AvatarActionPointModifier(int actionPoint)
        {
            this.actionPoint = actionPoint;
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarActionPointModifier m))
            {
                return;
            }

            actionPoint += m.actionPoint;
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarActionPointModifier m))
            {
                return;
            }

            actionPoint -= m.actionPoint;
        }

        public override AvatarState Modify(AvatarState state)
        {
            return state;

            // if (state is null)
            // {
            //     return null;
            // }
            //
            // state.actionPoint += actionPoint;
            // return state;
        }

        public override string ToString()
        {
            return $"{nameof(actionPoint)}: {actionPoint}";
        }
    }
}
