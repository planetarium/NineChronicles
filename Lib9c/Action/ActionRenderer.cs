using System;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain.Renderers;
using Libplanet.Blocks;
using static Nekoyume.Action.ActionBase;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Nekoyume.Action
{
    using Action = PolymorphicAction<ActionBase>;

    // FIXME: As it renders more than actions now, we should rename this class.
    public class ActionRenderer : IRenderer<Action>
    {
        private readonly Subject<ActionEvaluation<ActionBase>> _actionRenderSubject =
            new Subject<ActionEvaluation<ActionBase>>();

        private readonly Subject<ActionEvaluation<ActionBase>> _actionUnrenderSubject =
            new Subject<ActionEvaluation<ActionBase>>();

        private readonly Subject<(Block<Action> OldTip, Block<Action> NewTip)> _blockSubject =
            new Subject<(Block<Action> OldTip, Block<Action> NewTip)>();

        private readonly
        Subject<(Block<Action> OldTip, Block<Action> NewTip, Block<Action> Branchpoint)>
        _reorgSubject =
            new Subject<(Block<Action> OldTip, Block<Action> NewTip, Block<Action> Branchpoint)>();

        public void RenderAction(
            IAction action,
            IActionContext context,
            IAccountStateDelta nextStates
        )
        {
            if (action is Action polymorphicAction)
            {
                _actionRenderSubject.OnNext(new ActionBase.ActionEvaluation<ActionBase>()
                {
                    Action = polymorphicAction.InnerAction,
                    Signer = context.Signer,
                    BlockIndex = context.BlockIndex,
                    OutputStates = nextStates,
                    PreviousStates = context.PreviousStates,
                });
            }
        }

        public void UnrenderAction(
            IAction action,
            IActionContext context,
            IAccountStateDelta nextStates
        )
        {
            if (action is Action polymorphicAction)
            {
                _actionUnrenderSubject.OnNext(new ActionBase.ActionEvaluation<ActionBase>()
                {
                    Action = polymorphicAction.InnerAction,
                    Signer = context.Signer,
                    BlockIndex = context.BlockIndex,
                    OutputStates = nextStates,
                    PreviousStates = context.PreviousStates,
                });
            }
        }

        public void RenderActionError(
            IAction action,
            IActionContext context,
            Exception exception
        )
        {
            if (action is Action polymorphicAction)
            {
                _actionRenderSubject.OnNext(new ActionBase.ActionEvaluation<ActionBase>()
                {
                    Action = polymorphicAction.InnerAction,
                    Signer = context.Signer,
                    BlockIndex = context.BlockIndex,
                    OutputStates = context.PreviousStates,
                    Exception = exception,
                    PreviousStates = context.PreviousStates,
                });
            }
        }

        public void UnrenderActionError(
            IAction action,
            IActionContext context,
            Exception exception
        )
        {
            if (action is Action polymorphicAction)
            {
                _actionUnrenderSubject.OnNext(new ActionBase.ActionEvaluation<ActionBase>()
                {
                    Action = polymorphicAction.InnerAction,
                    Signer = context.Signer,
                    BlockIndex = context.BlockIndex,
                    OutputStates = context.PreviousStates,
                    Exception = exception,
                    PreviousStates = context.PreviousStates,
                });
            }
        }

        public void RenderBlock(
            Block<Action> oldTip,
            Block<Action> newTip
        )
        {
            _blockSubject.OnNext((oldTip, newTip));
        }

        public void RenderReorg(
            Block<Action> oldTip,
            Block<Action> newTip,
            Block<Action> branchpoint
        )
        {
            _reorgSubject.OnNext((oldTip, newTip, branchpoint));
        }

        public IObservable<ActionEvaluation<T>> EveryRender<T>()
            where T : ActionBase
        {
            return _actionRenderSubject.AsObservable().Where(
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
            return _actionUnrenderSubject.AsObservable().Where(
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
            return _actionRenderSubject.AsObservable().Where(
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
            return _actionUnrenderSubject.AsObservable().Where(
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

        public IObservable<(Block<Action> OldTip, Block<Action> NewTip)> EveryBlock() =>
            _blockSubject.AsObservable();

        public IObservable<(Block<Action> OldTip, Block<Action> NewTip, Block<Action> Branchpoint)>
        EveryReorg() =>
            _reorgSubject.AsObservable();
    }
}
