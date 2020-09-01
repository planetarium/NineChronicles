using System;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain.Renderers;
using Libplanet.Blocks;
using static Nekoyume.Action.ActionBase;
using Serilog;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Nekoyume.Action
{
    using NCAction = PolymorphicAction<ActionBase>;
    using NCBlock = Block<PolymorphicAction<ActionBase>>;

    // FIXME: As it renders more than actions now, we should rename this class.
    public class ActionRenderer : IRenderer<NCAction>
    {
        public Subject<ActionEvaluation<ActionBase>> ActionRenderSubject { get; }
            = new Subject<ActionEvaluation<ActionBase>>();

        public Subject<ActionEvaluation<ActionBase>> ActionUnrenderSubject { get; }
            = new Subject<ActionEvaluation<ActionBase>>();

        private readonly Subject<(NCBlock OldTip, NCBlock NewTip)> _blockSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip)>();

        private readonly Subject<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)> _reorgSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)>();

        public void RenderAction(
            IAction action,
            IActionContext context,
            IAccountStateDelta nextStates
        )
        {
            ActionRenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = GetActionBase(action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = nextStates,
                PreviousStates = context.PreviousStates,
            });
        }

        public void UnrenderAction(
            IAction action,
            IActionContext context,
            IAccountStateDelta nextStates
        )
        {
            ActionUnrenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = GetActionBase(action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = nextStates,
                PreviousStates = context.PreviousStates,
            });
        }

        public void RenderActionError(
            IAction action,
            IActionContext context,
            Exception exception
        )
        {
            Log.Error(exception, "{action} exeuction failed.", action);
            ActionRenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = GetActionBase(action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = context.PreviousStates,
                Exception = exception,
                PreviousStates = context.PreviousStates,
            });
        }

        public void UnrenderActionError(
            IAction action,
            IActionContext context,
            Exception exception
        )
        {
            ActionUnrenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = GetActionBase(action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = context.PreviousStates,
                Exception = exception,
                PreviousStates = context.PreviousStates,
            });
        }

        public void RenderBlock(
            NCBlock oldTip,
            NCBlock newTip
        )
        {
            _blockSubject.OnNext((oldTip, newTip));
        }

        public void RenderReorg(
            NCBlock oldTip,
            NCBlock newTip,
            NCBlock branchpoint
        )
        {
            _reorgSubject.OnNext((oldTip, newTip, branchpoint));
        }

        public IObservable<ActionEvaluation<T>> EveryRender<T>()
            where T : ActionBase
        {
            return ActionRenderSubject.AsObservable().Where(
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
            return ActionUnrenderSubject.AsObservable().Where(
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
            return ActionRenderSubject.AsObservable().Where(
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
            return ActionUnrenderSubject.AsObservable().Where(
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

        public IObservable<(NCBlock OldTip, NCBlock NewTip)> EveryBlock() =>
            _blockSubject.AsObservable();

        public IObservable<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)>
            EveryReorg() =>
            _reorgSubject.AsObservable();

        private static ActionBase GetActionBase(IAction action)
        {
            if (action is NCAction polymorphicAction)
            {
                return polymorphicAction.InnerAction;
            }
            return (ActionBase) action;
        }
    }
}
