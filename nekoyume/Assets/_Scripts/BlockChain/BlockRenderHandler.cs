using System;
using System.Collections.Generic;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c.Renderer;
using Libplanet.Action;
using Libplanet.Assets;
using Libplanet.Blocks;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Nekoyume.BlockChain
{
    using Nekoyume.Helper;
    using Nekoyume.UI.Scroller;
    using UniRx;
    using NCAction = PolymorphicAction<ActionBase>;
    using NCBlock = Block<PolymorphicAction<ActionBase>>;

    public class BlockRenderHandler
    {
        private static class Singleton
        {
            internal static readonly BlockRenderHandler Value = new BlockRenderHandler();
        }

        public static BlockRenderHandler Instance => Singleton.Value;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private BlockRenderer _blockRenderer;
        private static bool _balanceUpdateRequired = true;
        private static bool _avatarUpdateRequired = true;

        private BlockRenderHandler()
        {
        }

        public void Start(BlockRenderer blockRenderer)
        {
            _blockRenderer = blockRenderer ?? throw new ArgumentNullException(nameof(blockRenderer));

            Stop();
            _blockRenderer.BlockSubject
                .ObserveOnMainThread()
                .Subscribe(_ => UpdateWhenEveryBlockRenderBeginningAsync().Forget())
                .AddTo(_disposables);
            _blockRenderer.ReorgSubject
                .ObserveOnMainThread()
                .Subscribe(_ =>
                {
                    var msg = L10nManager.Localize("ERROR_REORG_OCCURRED");
                    UI.NotificationSystem.Push(Model.Mail.MailType.System, msg, NotificationCell.NotificationType.Alert);
                })
                .AddTo(_disposables);
            _blockRenderer.ReorgEndSubject.ObserveOnMainThread().Subscribe(_ =>
            {
                Debug.Log($"[{nameof(BlockRenderHandler)}] Reorg end");
            }).AddTo(_disposables);
            if (Game.Game.instance.Agent is RPCAgent rpcAgent)
            {
                rpcAgent.OnRetryStarted.Subscribe(_ =>
                {
                    _balanceUpdateRequired = true;
                    _avatarUpdateRequired = true;
                }).AddTo(_disposables);
            }
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private static async UniTaskVoid UpdateWhenEveryBlockRenderBeginningAsync()
        {
            var agent = Game.Game.instance.Agent;
            if (agent is null)
            {
                return;
            }

            var agentState = States.Instance.AgentState;
            if (agentState != null && _balanceUpdateRequired)
            {
                var (hasException, exception) = await UniTask.Run<(bool hasException, Exception exception)>(async () =>
                {
                    FungibleAssetValue gold;
                    FungibleAssetValue crystal;
                    try
                    {
                        gold = await agent.GetBalanceAsync(
                            agentState.address,
                            States.Instance.GoldBalanceState.Gold.Currency);
                        crystal = await agent.GetBalanceAsync(
                            agentState.address,
                            CrystalCalculator.CRYSTAL);
                    }
                    catch (Exception e)
                    {
                        return (true, e);
                    }

                    AgentStateSubject.OnNextGold(gold);
                    AgentStateSubject.OnNextCrystal(crystal);
                    _balanceUpdateRequired = false;
                    return (false, null);
                });
                if (hasException && !(exception is OperationCanceledException))
                {
                    Debug.LogException(exception);
                }
            }

            var currentAvatarState = States.Instance.CurrentAvatarState;
            if (currentAvatarState != null && _avatarUpdateRequired)
            {
                var (hasException, exception) = await UniTask.Run<(bool hasException, Exception exception)>(async () =>
                {
                    IValue value;
                    try
                    {
                        value = await agent.GetStateAsync(currentAvatarState.address);
                    }
                    catch (Exception e)
                    {
                        return (true, e);
                    }

                    if (!(value is Bencodex.Types.Dictionary dict))
                    {
                        return (true, new InvalidCastException(
                            $"value cannot cast to {typeof(Bencodex.Types.Dictionary).FullName}"));
                    }

                    var ap = dict.ContainsKey(ActionPointKey)
                        ? (int)(Bencodex.Types.Integer)dict[ActionPointKey]
                        : dict.ContainsKey(LegacyActionPointKey)
                            ? (int)(Bencodex.Types.Integer)dict[LegacyActionPointKey]
                            : 0;
                    ReactiveAvatarState.UpdateActionPoint(ap);

                    var bi = dict.ContainsKey(DailyRewardReceivedIndexKey)
                        ? (int)(Bencodex.Types.Integer)dict[DailyRewardReceivedIndexKey]
                        : dict.ContainsKey(LegacyDailyRewardReceivedIndexKey)
                            ? (int)(Bencodex.Types.Integer)dict[LegacyDailyRewardReceivedIndexKey]
                            : 0;
                    ReactiveAvatarState.UpdateDailyRewardReceivedIndex(bi);
                    _avatarUpdateRequired = false;
                    return (false, null);
                });
                if (hasException && !(exception is OperationCanceledException))
                {
                    Debug.LogException(exception);
                }
            }

            // NOTE: Unregister actions created before 300 blocks for optimization.
            // 300 * 12s = 3600s = 1h
            LocalLayerActions.Instance.UnregisterCreatedBefore(agent.BlockIndex - 1000);
        }
    }
}
