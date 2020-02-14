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
        
        private static readonly Subject<ActionEvaluation<ActionBase>> RenderSubject =
            new Subject<ActionEvaluation<ActionBase>>();

        private static readonly Subject<ActionEvaluation<ActionBase>> UnrenderSubject =
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

        public static IObservable<ActionEvaluation<T>> EveryRender<T>()
            where T : ActionBase
        {
            return RenderSubject.AsObservable().Where(
                eval => eval.Action is T
            ).Select(eval => new ActionEvaluation<T>
            {
                Action = (T) eval.Action,
                Signer = eval.Signer,
                BlockIndex = eval.BlockIndex,
                OutputStates = eval.OutputStates,
            });
        }

        public static IObservable<ActionEvaluation<T>> EveryUnrender<T>()
            where T : ActionBase
        {
            return UnrenderSubject.AsObservable().Where(
                eval => eval.Action is T
            ).Select(eval => new ActionEvaluation<T>
            {
                Action = (T) eval.Action,
                Signer = eval.Signer,
                BlockIndex = eval.BlockIndex,
                OutputStates = eval.OutputStates,
            });
        }

        public static IObservable<ActionEvaluation<ActionBase>> EveryRender(Address updatedAddress)
        {
            return RenderSubject.AsObservable().Where(
                eval => eval.OutputStates.UpdatedAddresses.Contains(updatedAddress)
            ).Select(eval => new ActionEvaluation<ActionBase>
            {
                Action = eval.Action,
                Signer = eval.Signer,
                BlockIndex = eval.BlockIndex,
                OutputStates = eval.OutputStates,
            });
        }

        public static IObservable<ActionEvaluation<ActionBase>> EveryUnrender(Address updatedAddress)
        {
            return UnrenderSubject.AsObservable().Where(
                eval => eval.OutputStates.UpdatedAddresses.Contains(updatedAddress)
            ).Select(eval => new ActionEvaluation<ActionBase>
            {
                Action = eval.Action,
                Signer = eval.Signer,
                BlockIndex = eval.BlockIndex,
                OutputStates = eval.OutputStates,
            });
        }
    }
}
