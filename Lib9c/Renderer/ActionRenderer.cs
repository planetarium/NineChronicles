using System;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain.Renderers;
using Libplanet.Blocks;
using Nekoyume.Action;
using static Nekoyume.Action.ActionBase;
using Serilog;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Lib9c.Renderer
{
    using NCAction = PolymorphicAction<ActionBase>;
    using NCBlock = Block<PolymorphicAction<ActionBase>>;

    public class ActionRenderer : IActionRenderer<NCAction>
    {
        public Subject<ActionEvaluation<ActionBase>> ActionRenderSubject { get; }
            = new Subject<ActionEvaluation<ActionBase>>();

        public Subject<ActionEvaluation<ActionBase>> ActionUnrenderSubject { get; }
            = new Subject<ActionEvaluation<ActionBase>>();

        public readonly Subject<(NCBlock OldTip, NCBlock NewTip)> BlockEndSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip)>();

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
                RandomSeed = context.Random.Seed
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
                RandomSeed = context.Random.Seed
            });
        }

        public void RenderActionError(
            IAction action,
            IActionContext context,
            Exception exception
        )
        {
            Log.Error(exception, "{action} execution failed.", action);
            ActionRenderSubject.OnNext(new ActionEvaluation<ActionBase>()
            {
                Action = GetActionBase(action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = context.PreviousStates,
                Exception = exception,
                PreviousStates = context.PreviousStates,
                RandomSeed = context.Random.Seed
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
                RandomSeed = context.Random.Seed
            });
        }

        public void RenderBlock(
            NCBlock oldTip,
            NCBlock newTip
        )
        {
            // RenderBlock should be handled by BlockRenderer
        }

        public void RenderBlockEnd(
            NCBlock oldTip,
            NCBlock newTip
        )
        {
            BlockEndSubject.OnNext((oldTip, newTip));
        }

        public void RenderReorg(
            NCBlock oldTip,
            NCBlock newTip,
            NCBlock branchpoint
        )
        {
            // RenderReorg should be handled by BlockRenderer
        }

        public void RenderReorgEnd(
            NCBlock oldTip,
            NCBlock newTip,
            NCBlock branchpoint
        )
        {
            // RenderReorgEnd should be handled by BlockRenderer
        }

        public IObservable<ActionEvaluation<T>> EveryRender<T>()
            where T : ActionBase
        {
            return ActionRenderSubject.AsObservable().Where(
                eval => eval.Action is T
            ).Select(eval => new ActionEvaluation<T>
            {
                Action = (T)eval.Action,
                Signer = eval.Signer,
                BlockIndex = eval.BlockIndex,
                OutputStates = eval.OutputStates,
                Exception = eval.Exception,
                PreviousStates = eval.PreviousStates,
                RandomSeed = eval.RandomSeed
            });
        }

        public IObservable<ActionEvaluation<T>> EveryUnrender<T>()
            where T : ActionBase
        {
            return ActionUnrenderSubject.AsObservable().Where(
                eval => eval.Action is T
            ).Select(eval => new ActionEvaluation<T>
            {
                Action = (T)eval.Action,
                Signer = eval.Signer,
                BlockIndex = eval.BlockIndex,
                OutputStates = eval.OutputStates,
                Exception = eval.Exception,
                PreviousStates = eval.PreviousStates,
                RandomSeed = eval.RandomSeed
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
                RandomSeed = eval.RandomSeed
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
                RandomSeed = eval.RandomSeed
            });
        }

        public IObservable<(NCBlock OldTip, NCBlock NewTip)> EveryBlockEnd() =>
            BlockEndSubject.AsObservable();

        private static ActionBase GetActionBase(IAction action)
        {
            if (action is NCAction polymorphicAction)
            {
                return polymorphicAction.InnerAction;
            }
            return (ActionBase)action;
        }
    }
}
