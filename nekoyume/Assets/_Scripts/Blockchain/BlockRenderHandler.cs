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

        /// <summary>
        /// 다음 블록 렌더에서 BoostScheduleSheet 를 노드 값으로 다시 맞춘다. 패치 렌더를 놓칠 수 있는 구간(부팅 중,
        /// RPC 재접속 사이)이 끝날 때 켠다. 처음엔 켜 둔다.
        /// </summary>
        private static bool _boostScheduleSheetRefreshRequired = true;
        private static bool _boostScheduleSheetRefreshing;

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
                    RequestBoostScheduleSheetRefresh();
                }).AddTo(_disposables);
            }
        }

        /// <summary>
        /// 다음 블록 렌더에서 BoostScheduleSheet 를 노드에서 다시 받게 한다. 패치 렌더를 놓쳤을 수 있는 구간이
        /// 끝났을 때(재접속, TableSheets 대입) 부른다.
        /// </summary>
        public static void RequestBoostScheduleSheetRefresh()
        {
            // 재조회가 끝날 때까지 결과 재현은 지금 시트 대신 액션 직전 상태의 시트를 읽는다. 세대를 먼저 올려야
            // 그 사이 시작한 재조회가 이 요청까지 확인한 것으로 지우지 않는다.
            Game.Game.instance?.TableSheets?.MarkBoostScheduleSheetUnverified();
            _boostScheduleSheetRefreshRequired = true;
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private static async UniTaskVoid RefreshBoostScheduleSheetAsync()
        {
            _boostScheduleSheetRefreshing = true;
            // 조회 중에 다시 요청되면(재접속이 겹침) 그 요청은 남겨 다음 블록에 한 번 더 받는다.
            _boostScheduleSheetRefreshRequired = false;
            try
            {
                if (!await BoostScheduleFrontHelper.RefreshFromNodeAsync())
                {
                    _boostScheduleSheetRefreshRequired = true; // 실패하면 다음 블록에 다시 시도한다.
                }
            }
            finally
            {
                _boostScheduleSheetRefreshing = false;
            }
        }

        private static async UniTaskVoid UpdateWhenEveryBlockRenderBeginningAsync()
        {
            var agent = Game.Game.instance.Agent;
            if (agent is null)
            {
                return;
            }

            if (_boostScheduleSheetRefreshRequired &&
                !_boostScheduleSheetRefreshing &&
                Game.Game.instance.TableSheets is not null)
            {
                RefreshBoostScheduleSheetAsync().Forget();
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
