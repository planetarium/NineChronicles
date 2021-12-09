using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Lib9c.Renderer;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    using UniRx;

    // NOTE: Ref https://docs.libplanet.io/0.22.0/api/Libplanet.Blockchain.Renderers.IActionRenderer-1.html
    public class BlockChainBehaviour
    {
        private readonly IAgent _agent;
        private readonly BlockRenderer _blockRenderer;
        private readonly ActionRenderer _actionRenderer;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private bool _isStarted;
        private bool _isInReorg;
        private readonly List<Address> _updatedAddresses = new List<Address>();
        

        public BlockChainBehaviour(IAgent agent, BlockRenderer blockRenderer, ActionRenderer actionRenderer)
        {
            _agent = agent;
            _blockRenderer = blockRenderer;
            _actionRenderer = actionRenderer;
        }

        public void Start()
        {
            if (_isStarted)
            {
                return;
            }

            _isStarted = true;

            _blockRenderer.ReorgSubject
                .ObserveOnMainThread()
                .Subscribe(tuple =>
                {
                    Debug.Log(
                        $"[{nameof(BlockChainBehaviour)}] Reorg block beginning: {tuple.OldTip.Index} -> {tuple.NewTip.Index} | branch point: {tuple.Branchpoint.Index}");

                    _isInReorg = true;
                    _updatedAddresses.Clear();

                    var msg = L10nManager.Localize("ERROR_REORG_OCCURRED");
                    UI.NotificationSystem.Push(
                        Model.Mail.MailType.System,
                        msg,
                        NotificationCell.NotificationType.Information);
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

                    foreach (var address in eval.OutputStates.UpdatedAddresses)
                    {
                        if (_updatedAddresses.Contains(address))
                        {
                            continue;
                        }
                        
                        _updatedAddresses.Add(address);
                    }

                    LocalLayerActions.Instance.SetRendered(gameAction.Id, false);
                })
                .AddTo(_disposables);

            _blockRenderer.BlockSubject
                .ObserveOnMainThread()
                .Subscribe(tuple =>
                {
                    Debug.Log(
                        $"[{nameof(BlockChainBehaviour)}] Render block beginning: {tuple.OldTip.Index} -> {tuple.NewTip.Index}");

                    if (_isInReorg)
                    {
                        return;
                    }

                    _updatedAddresses.Clear();

                    // NOTE: Unregister actions created before 300 blocks for optimization. 1h = 3600s = 12s * 300
                    LocalLayerActions.Instance.UnregisterCreatedBefore(Game.Game.instance.Agent.BlockIndex - 300);
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

                    foreach (var address in eval.OutputStates.UpdatedAddresses)
                    {
                        if (_updatedAddresses.Contains(address))
                        {
                            continue;
                        }
                        
                        _updatedAddresses.Add(address);
                    }

                    LocalLayerActions.Instance.SetRendered(gameAction.Id, true);
                })
                .AddTo(_disposables);

            _actionRenderer.BlockEndSubject
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    Debug.Log(_isInReorg
                        ? $"[{nameof(BlockChainBehaviour)}] Render all actions end"
                        : $"[{nameof(BlockChainBehaviour)}] Render all actions end in block reorg");

                    if (_isInReorg)
                    {
                        return;
                    }

#pragma warning disable 4014
                    UpdateGameStateAsync();
#pragma warning restore 4014
                })
                .AddTo(_disposables);

            _blockRenderer.ReorgEndSubject
                .ObserveOnMainThread()
                .Subscribe(tuple =>
                {
                    Debug.Log(
                        $"[{nameof(BlockChainBehaviour)}] Reorg block end: {tuple.OldTip.Index} -> {tuple.NewTip.Index} | branch point: {tuple.Branchpoint.Index}");

                    _isInReorg = false;

#pragma warning disable 4014
                    UpdateGameStateAsync();
#pragma warning restore 4014
                })
                .AddTo(_disposables);
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private async UniTask UpdateGameStateAsync()
        {
            if (States.Instance.AgentState is null)
            {
                return;
            }

            // Update States
            await UniTask.Run(async () => await States.Instance.UpdateAsync(_agent, _updatedAddresses, true));

            // LocalLayerActions 적용
            LocalLayerActions.Instance.Apply(_agent, States.Instance, TableSheets.Instance, _updatedAddresses, true);

            // LocalLayerCommands 적용
            LocalLayerCommands.Instance.Apply(_agent, States.Instance, TableSheets.Instance, _updatedAddresses, true);

            // Notify all once
            AgentStateSubject.OnNextGold(States.Instance.GoldBalanceState.Gold);

            for (var i = 0; i < _updatedAddresses.Count; i++)
            {
                var updatedAddress = _updatedAddresses[i];
                
                // AgentState
                if (updatedAddress.Equals(States.Instance.AgentState.address))
                {
                    continue;
                }
                
                // CurrentAvatarState
                if (updatedAddress.Equals(States.Instance.CurrentAvatarState.address))
                {
                    ReactiveAvatarState.Initialize(States.Instance.CurrentAvatarState);
                    continue;
                }

                // TODO: notify CombinationSlotStates
                // foreach (var pair in States.Instance.CombinationSlotStates)
                // {
                //     break;
                // }

                // GameConfigState
                if (updatedAddress.Equals(States.Instance.GameConfigState.address))
                {
                    GameConfigStateSubject.OnNext(States.Instance.GameConfigState);
                }
            }

            // 기존 구독 로직 실행.
            // for (var i = 0; i < _responseActions.Count; i++)
            // {
            //     var action = _responseActions[i];
            //     action();
            // }
        }
    }
}
