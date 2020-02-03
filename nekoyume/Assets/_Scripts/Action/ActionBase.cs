using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
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
            public IActionContext InputContext { get; set; }
            public IAccountStateDelta OutputStates { get; set; }
        }

        // FIXME Unity / 라이브러리에서 모두 사용 가능하게 구조를 정리해야 합니다.
        #if UNITY_EDITOR || UNITY_STANDALONE
        private static readonly Subject<ActionEvaluation<ActionBase>> RenderSubject =
            new Subject<ActionEvaluation<ActionBase>>();

        private static readonly Subject<ActionEvaluation<ActionBase>> UnrenderSubject =
            new Subject<ActionEvaluation<ActionBase>>();

        public void Render(IActionContext context, IAccountStateDelta nextStates)
        {
            RenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = this,
                InputContext = context,
                OutputStates = nextStates,
            });
        }

        public void Unrender(IActionContext context, IAccountStateDelta nextStates)
        {
            UnrenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = this,
                InputContext = context,
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
                InputContext = eval.InputContext,
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
                InputContext = eval.InputContext,
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
                InputContext = eval.InputContext,
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
                InputContext = eval.InputContext,
                OutputStates = eval.OutputStates,
            });
        }
        #else
        public void Render(IActionContext context, IAccountStateDelta nextStates)
        {
        }

        public void Unrender(IActionContext context, IAccountStateDelta nextStates)
        {
        }
        #endif
    }
}
