using System;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Action;

namespace NineChronicles.Standalone.Tests.Common.Actions
{
    // 테스트를 위해 만든 RewardGold 액션입니다.
    class RewardGold : IAction
    {
        public void LoadPlainValue(IValue plainValue)
        {
        }

        public IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states.SetState(context.Signer, default(Null));
            }

            var gold = states.TryGetState(context.Signer, out Integer integer) ? integer : (Integer)0;
            gold += 1;

            return states.SetState(context.Signer, gold);
        }

        public void Render(IActionContext context, IAccountStateDelta nextStates)
        {
        }

        public void RenderError(IActionContext context, Exception exception)
        {
        }

        public void Unrender(IActionContext context, IAccountStateDelta nextStates)
        {
        }

        public void UnrenderError(IActionContext context, Exception exception)
        {
        }

        public IValue PlainValue => new Null();
    }
}
