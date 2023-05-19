using System;
using Libplanet;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Blockchain.Renderers;
using Libplanet.Blocks;
using Libplanet.State;
using Nekoyume.Action;
using Serilog;
using Bencodex.Types;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Lib9c.Renderers
{
    public class ActionRenderer : IActionRenderer
    {
        private readonly IActionLoader _actionLoader;

        public Subject<ActionEvaluation<ActionBase>> ActionRenderSubject { get; }
            = new Subject<ActionEvaluation<ActionBase>>();

        public Subject<ActionEvaluation<ActionBase>> ActionUnrenderSubject { get; }
            = new Subject<ActionEvaluation<ActionBase>>();

        public readonly Subject<(Block OldTip, Block NewTip)> BlockEndSubject =
            new Subject<(Block OldTip, Block NewTip)>();

        public ActionRenderer()
        {
            _actionLoader = TypedActionLoader.Create(typeof(ActionBase).Assembly, typeof(ActionBase));
        }

        public void RenderAction(IValue action, IActionContext context, IAccountStateDelta nextStates) =>
            ActionRenderSubject.OnNext(new ActionEvaluation<ActionBase>
            {
                Action = context.BlockAction
                    ? new RewardGold()
                    : (ActionBase)_actionLoader.LoadAction(context.BlockIndex, action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                TxId = context.TxId,
                OutputStates = nextStates,
                PreviousStates = context.PreviousStates,
                RandomSeed = context.Random.Seed
            });

        public void UnrenderAction(IValue action, IActionContext context, IAccountStateDelta nextStates) =>
            ActionUnrenderSubject.OnNext(new ActionEvaluation<ActionBase>
            {
                Action = context.BlockAction
                    ? new RewardGold()
                    : (ActionBase)_actionLoader.LoadAction(context.BlockIndex, action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                TxId = context.TxId,
                OutputStates = nextStates,
                PreviousStates = context.PreviousStates,
                RandomSeed = context.Random.Seed
            });

        public void RenderActionError(
            IValue action,
            IActionContext context,
            Exception exception
        )
        {
            Log.Error(exception, "{action} execution failed.", action);
            ActionRenderSubject.OnNext(new ActionEvaluation<ActionBase>
            {
                Action = context.BlockAction
                    ? new RewardGold()
                    : (ActionBase)_actionLoader.LoadAction(context.BlockIndex, action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                TxId = context.TxId,
                OutputStates = context.PreviousStates,
                Exception = exception,
                PreviousStates = context.PreviousStates,
                RandomSeed = context.Random.Seed
            });
        }

        public void UnrenderActionError(IValue action, IActionContext context, Exception exception) =>
            ActionUnrenderSubject.OnNext(new ActionEvaluation<ActionBase>
            {
                Action = context.BlockAction
                    ? new RewardGold()
                    : (ActionBase)_actionLoader.LoadAction(context.BlockIndex, action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                TxId = context.TxId,
                OutputStates = context.PreviousStates,
                Exception = exception,
                PreviousStates = context.PreviousStates,
                RandomSeed = context.Random.Seed
            });

        [Obsolete("Use BlockRenderer.RenderBlock(oldTip, newTip)")]
        public void RenderBlock(Block oldTip, Block newTip)
        {
            // RenderBlock should be handled by BlockRenderer
        }

        public void RenderBlockEnd(Block oldTip, Block newTip)
        {
            BlockEndSubject.OnNext((oldTip, newTip));
        }

        public IObservable<ActionEvaluation<T>> EveryRender<T>() where T : ActionBase =>
            ActionRenderSubject
                .AsObservable()
                .Where(eval => eval.Action is T)
                .Select(eval => new ActionEvaluation<T>
                {
                    Action = (T)eval.Action,
                    Signer = eval.Signer,
                    BlockIndex = eval.BlockIndex,
                    TxId = eval.TxId,
                    OutputStates = eval.OutputStates,
                    Exception = eval.Exception,
                    PreviousStates = eval.PreviousStates,
                    RandomSeed = eval.RandomSeed,
                    Extra = eval.Extra,
                });

        public IObservable<ActionEvaluation<T>> EveryUnrender<T>() where T : ActionBase =>
            ActionUnrenderSubject
                .AsObservable()
                .Where(eval => eval.Action is T)
                .Select(eval => new ActionEvaluation<T>
                {
                    Action = (T)eval.Action,
                    Signer = eval.Signer,
                    BlockIndex = eval.BlockIndex,
                    TxId = eval.TxId,
                    OutputStates = eval.OutputStates,
                    Exception = eval.Exception,
                    PreviousStates = eval.PreviousStates,
                    RandomSeed = eval.RandomSeed
                });

        public IObservable<ActionEvaluation<ActionBase>> EveryRender(Address updatedAddress) =>
            ActionRenderSubject
                .AsObservable()
                .Where(eval => eval.OutputStates.UpdatedAddresses.Contains(updatedAddress))
                .Select(eval => new ActionEvaluation<ActionBase>
                {
                    Action = eval.Action,
                    Signer = eval.Signer,
                    BlockIndex = eval.BlockIndex,
                    TxId = eval.TxId,
                    OutputStates = eval.OutputStates,
                    Exception = eval.Exception,
                    PreviousStates = eval.PreviousStates,
                    RandomSeed = eval.RandomSeed,
                    Extra = eval.Extra,
                });

        public IObservable<ActionEvaluation<ActionBase>> EveryUnrender(Address updatedAddress) =>
            ActionUnrenderSubject
                .AsObservable()
                .Where(eval => eval.OutputStates.UpdatedAddresses.Contains(updatedAddress))
                .Select(eval => new ActionEvaluation<ActionBase>
                {
                    Action = eval.Action,
                    Signer = eval.Signer,
                    BlockIndex = eval.BlockIndex,
                    TxId = eval.TxId,
                    OutputStates = eval.OutputStates,
                    Exception = eval.Exception,
                    PreviousStates = eval.PreviousStates,
                    RandomSeed = eval.RandomSeed,
                    Extra = eval.Extra,
                });
    }
}
