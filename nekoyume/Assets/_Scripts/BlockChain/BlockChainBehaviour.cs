using System;
using System.Collections.Generic;
using Lib9c.Renderer;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    using UniRx;

    public class BlockChainBehaviour
    {
        private enum State
        {
            Stopped,
            Started,
            InBlockRender,
            InBlockReorg,
        }

        private State _state = State.Stopped;

        private readonly BlockRenderer _blockRenderer;
        private readonly ActionRenderer _actionRenderer;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public BlockChainBehaviour(BlockRenderer blockRenderer, ActionRenderer actionRenderer)
        {
            _blockRenderer = blockRenderer;
            _actionRenderer = actionRenderer;
        }

        public void Start()
        {
            if (_state != State.Stopped)
            {
                return;
            }

            _state = State.Started;

            _blockRenderer.BlockSubject
                .ObserveOnMainThread()
                .Subscribe(tuple =>
                {
                    Debug.Log(
                        $"[{nameof(BlockChainBehaviour)}] Render block beginning: {tuple.OldTip.Index} -> {tuple.NewTip.Index}");
                    _state = State.InBlockRender;

                    // NOTE: Unregister actions created before 300 blocks for optimization. 1h = 3600s = 12s * 300
                    LocalLayerActions.Instance.UnregisterCreatedBefore(Game.Game.instance.Agent.BlockIndex - 300);
                })
                .AddTo(_disposables);

            _blockRenderer.ReorgSubject
                .ObserveOnMainThread()
                .Subscribe(tuple =>
                {
                    Debug.Log(
                        $"[{nameof(BlockChainBehaviour)}] Reorg block beginning: {tuple.OldTip.Index} -> {tuple.NewTip.Index} | branch point: {tuple.Branchpoint.Index}");
                    _state = State.InBlockReorg;

                    var msg = L10nManager.Localize("ERROR_REORG_OCCURRED");
                    UI.NotificationSystem.Push(Model.Mail.MailType.System, msg);
                })
                .AddTo(_disposables);

            _blockRenderer.ReorgEndSubject
                .ObserveOnMainThread()
                .Subscribe(tuple =>
                {
                    Debug.Log(
                        $"[{nameof(BlockChainBehaviour)}] Reorg block end: {tuple.OldTip.Index} -> {tuple.NewTip.Index} | branch point: {tuple.Branchpoint.Index}");
                    _state = State.Started;
                })
                .AddTo(_disposables);

            _actionRenderer.ActionRenderSubject
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    if (!(eval.Action is GameAction gameAction))
                    {
                        return;
                    }

                    Debug.Log(
                        $"[{nameof(BlockChainBehaviour)}] Render action: {gameAction.GetType().Name} {gameAction.Id.ToString()}");

                    LocalLayerActions.Instance.SetRendered(gameAction.Id, true);
                })
                .AddTo(_disposables);

            _actionRenderer.ActionUnrenderSubject
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    if (!(eval.Action is GameAction gameAction))
                    {
                        return;
                    }

                    Debug.Log(
                        $"[{nameof(BlockChainBehaviour)}] Unrender action: {gameAction.GetType().Name} {gameAction.Id.ToString()}");

                    LocalLayerActions.Instance.SetRendered(gameAction.Id, false);
                })
                .AddTo(_disposables);

            _actionRenderer.BlockEndSubject
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    if (_state == State.InBlockReorg)
                    {
                        Debug.Log($"[{nameof(BlockChainBehaviour)}] Render all actions end in reorg block");
                        return;
                    }

                    Debug.Log($"[{nameof(BlockChainBehaviour)}] Render all actions end");

                    // States 초기화
                    LocalLayerActions.Instance.PayCost(Game.Game.instance.Agent, States.Instance, TableSheets.Instance);
                    // 기존 구독 로직 실행.
                    // for (var i = 0; i < _responseActions.Count; i++)
                    // {
                    //     var action = _responseActions[i];
                    //     action();
                    // }
                })
                .AddTo(_disposables);
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
