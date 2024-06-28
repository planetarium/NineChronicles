using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Lib9c;
using Lib9c.Renderers;
using Libplanet.Types.Assets;
using Nekoyume.Exceptions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;

namespace Nekoyume.Blockchain
{
    using UniRx;

    public class BlockRenderHandler
    {
        private static class Singleton
        {
            internal static readonly BlockRenderHandler Value = new();
        }

        public static BlockRenderHandler Instance => Singleton.Value;

        private readonly List<IDisposable> _disposables = new();

        private BlockRenderer _blockRenderer;
        private static bool _balanceUpdateRequired = true;
        private static bool _avatarUpdateRequired = true;

        private BlockRenderHandler()
        {
        }

        public void Start(BlockRenderer blockRenderer)
        {
            _blockRenderer =
                blockRenderer ?? throw new ArgumentNullException(nameof(blockRenderer));

            Stop();
            _blockRenderer.BlockSubject
                .ObserveOnMainThread()
                .Subscribe(_ => UpdateWhenEveryBlockRenderBeginningAsync().Forget())
                .AddTo(_disposables);
            if (Game.Game.instance.Agent is RPCAgent rpcAgent)
            {
                rpcAgent.OnRetryEnded.Subscribe(_ =>
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
                var exception = await UniTask.Run(async () =>
                {
                    FungibleAssetValue gold;
                    FungibleAssetValue crystal;
                    FungibleAssetValue garage;
                    try
                    {
                        var ncg = States.Instance.GoldBalanceState.Gold.Currency;
                        var favArr = await Task.WhenAll(
                            agent.GetBalanceAsync(agentState.address, ncg),
                            agent.GetBalanceAsync(agentState.address, Currencies.Crystal),
                            agent.GetBalanceAsync(agentState.address, Currencies.Garage));
                        gold = favArr[0];
                        crystal = favArr[1];
                        garage = favArr[2];
                    }
                    catch (Exception e)
                    {
                        return e;
                    }

                    AgentStateSubject.OnNextGold(gold);
                    AgentStateSubject.OnNextCrystal(crystal);
                    AgentStateSubject.OnNextGarage(garage);

                    _balanceUpdateRequired = false;
                    return null;
                });
                if (exception is not null && exception is not OperationCanceledException)
                {
                    NcDebug.LogException(exception);
                }
            }

            var currentAvatarState = States.Instance.CurrentAvatarState;
            if (currentAvatarState != null && _avatarUpdateRequired)
            {
                var exception = await UniTask.Run(async () =>
                {
                    AvatarState avatarState;
                    try
                    {
                        avatarState = (await agent.GetAvatarStatesAsync(
                            new[] { currentAvatarState.address }))[currentAvatarState.address];
                    }
                    catch (Exception e)
                    {
                        return e;
                    }

                    if (avatarState is null)
                    {
                        return new StateNullException(
                            $"Given address {currentAvatarState.address} is empty.");
                    }

                    await ActionRenderHandler.Instance.UpdateCurrentAvatarStateAsync(avatarState);

                    var states = await Task.WhenAll(
                        agent.GetStateAsync(Addresses.ActionPoint, avatarState.address),
                        agent.GetStateAsync(Addresses.DailyReward, avatarState.address));
                    var ap = states[0] is Integer apValue ? (long)apValue : avatarState.actionPoint;
                    ReactiveAvatarState.UpdateActionPoint(ap);
                    var dri = states[1] is Integer driValue ? (long)driValue : avatarState.dailyRewardReceivedIndex;
                    ReactiveAvatarState.UpdateDailyRewardReceivedIndex(dri);

                    var runeSheet = Game.Game.instance.TableSheets.RuneSheet;
                    var balances = await Task.WhenAll(runeSheet.Values.Select(row =>
                        agent.GetBalanceAsync(avatarState.address, RuneHelper.ToCurrency(row)))
                    );
                    foreach (var fav in balances)
                    {
                        States.Instance.SetCurrentAvatarBalance(fav);
                    }

                    _avatarUpdateRequired = false;
                    return null;
                });
                if (exception is not null && exception is not OperationCanceledException)
                {
                    NcDebug.LogException(exception);
                }
            }

            // NOTE: Unregister actions created before 300 blocks for optimization.
            // 300 * 12s = 3600s = 1h
            LocalLayerActions.Instance.UnregisterCreatedBefore(agent.BlockIndex - 1000);
        }
    }
}
