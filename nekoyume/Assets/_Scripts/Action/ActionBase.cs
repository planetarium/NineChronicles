using System;
using System.Collections.Immutable;
using Libplanet.Action;
using UniRx;

namespace Nekoyume.Action
{
    public abstract class ActionBase : IAction
    {
        public struct ErrorCode
        {
            public const int Success = 0;
            public const int Fail = -1;
            public const int KeyNotFoundInTable = -2;
        }

        public abstract IImmutableDictionary<string, object> PlainValue { get; }
        public abstract void LoadPlainValue(IImmutableDictionary<string, object> plainValue);
        public abstract IAccountStateDelta Execute(IActionContext context);

        public struct ActionEvaluation<T>
            where T : ActionBase
        {
            public T Action { get; set; }
            public IActionContext InputContext { get; set; }
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
    }
}
