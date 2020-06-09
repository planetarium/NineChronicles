using System;
using Libplanet;
using static Nekoyume.Action.ActionBase;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Nekoyume.Action
{
    public class ActionRenderer
    {
        private Subject<ActionEvaluation<ActionBase>> _renderSubject;

        private Subject<ActionEvaluation<ActionBase>> _unrenderSubject;

        public ActionRenderer(
            Subject<ActionEvaluation<ActionBase>> renderSubject,
            Subject<ActionEvaluation<ActionBase>> unrenderSubject
        )
        {
            _renderSubject = renderSubject;
            _unrenderSubject = unrenderSubject;
        }

        public IObservable<ActionEvaluation<T>> EveryRender<T>()
            where T : ActionBase
        {
            return _renderSubject.AsObservable().Where(
                eval => eval.Action is T
            ).Select(eval => new ActionEvaluation<T>
            {
                Action = (T) eval.Action,
                Signer = eval.Signer,
                BlockIndex = eval.BlockIndex,
                OutputStates = eval.OutputStates,
                Exception = eval.Exception,
                PreviousStates = eval.PreviousStates,
            });
        }

        public IObservable<ActionEvaluation<T>> EveryUnrender<T>()
            where T : ActionBase
        {
            return _unrenderSubject.AsObservable().Where(
                eval => eval.Action is T
            ).Select(eval => new ActionEvaluation<T>
            {
                Action = (T) eval.Action,
                Signer = eval.Signer,
                BlockIndex = eval.BlockIndex,
                OutputStates = eval.OutputStates,
                Exception = eval.Exception,
                PreviousStates = eval.PreviousStates,
            });
        }

        public IObservable<ActionEvaluation<ActionBase>> EveryRender(Address updatedAddress)
        {
            return _renderSubject.AsObservable().Where(
                eval => eval.OutputStates.UpdatedAddresses.Contains(updatedAddress)
            ).Select(eval => new ActionEvaluation<ActionBase>
            {
                Action = eval.Action,
                Signer = eval.Signer,
                BlockIndex = eval.BlockIndex,
                OutputStates = eval.OutputStates,
                Exception = eval.Exception,
                PreviousStates = eval.PreviousStates,
            });
        }

        public IObservable<ActionEvaluation<ActionBase>> EveryUnrender(Address updatedAddress)
        {
            return _unrenderSubject.AsObservable().Where(
                eval => eval.OutputStates.UpdatedAddresses.Contains(updatedAddress)
            ).Select(eval => new ActionEvaluation<ActionBase>
            {
                Action = eval.Action,
                Signer = eval.Signer,
                BlockIndex = eval.BlockIndex,
                OutputStates = eval.OutputStates,
                Exception = eval.Exception,
                PreviousStates = eval.PreviousStates,
            });
        }
    }
}
