using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Nekoyume.Action
{
    [Serializable]
    public abstract class ActionBase : IAction
    {
        public static readonly IValue MarkChanged = default(Null);

        public abstract IValue PlainValue { get; }
        public abstract void LoadPlainValue(IValue plainValue);
        public abstract IAccountStateDelta Execute(IActionContext ctx);

        public struct ActionEvaluation<T>
            where T : ActionBase
        {
            public T Action { get; set; }

            public Address Signer { get; set; }

            public long BlockIndex { get; set; }

            public IAccountStateDelta OutputStates { get; set; }
        }

        public static readonly Subject<ActionEvaluation<ActionBase>> RenderSubject =
            new Subject<ActionEvaluation<ActionBase>>();

        public static readonly Subject<ActionEvaluation<ActionBase>> UnrenderSubject =
            new Subject<ActionEvaluation<ActionBase>>();

        public void Render(IActionContext context, IAccountStateDelta nextStates)
        {
            RenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = this,
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = nextStates,
            });
        }

        public void Unrender(IActionContext context, IAccountStateDelta nextStates)
        {
            UnrenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = this,
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = nextStates,
            });
        }
    }
}
