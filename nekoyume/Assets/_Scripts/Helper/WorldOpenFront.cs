using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Libplanet.Types.Blocks;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Module;
using Nekoyume.TableData;
using UniRx;

namespace Nekoyume.Helper
{
    /// <summary>
    /// 하드월드 진행 개방 게이트(<see cref="WorldOpenGate"/>)를 클라에서 판정한다. 월드별로 지금 들어갈 수 있는 마지막
    /// 스테이지를 캐시하고, 진입 경로(월드맵·스테이지 정보·전투 결과·소탕·퀘스트)가 이 값으로 막는다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 판정은 lib9c 함수를 그대로 부른다. 클라에는 <see cref="IWorldState"/> 가 없으므로 게이트가 읽는 값(레거시 계정의
    /// <see cref="WorldOpenSheet"/>, <see cref="Addresses.WorldOpen"/> 계정의 개방 확정 블록)만 tip 상태에서 미리 읽어
    /// 그 값만 답하는 읽기 전용 상태(<see cref="CreateState"/>)를 만든다.
    /// </para>
    /// <para>
    /// 노드 조회는 <see cref="RefreshIntervalBlocks"/> 마다 하고, 판정은 읽어 둔 상태로 블록마다 tip + 1 에서 다시 한다 —
    /// fallback·유예로 열리는 구간이 조회 주기와 상관없이 제때 풀린다. 지금 보낸 tx 는 빨라야 다음 블록에 들어간다.
    /// 한 번도 못 읽었으면 <see cref="GameConfig.ProgressGatedStageBegin"/> 이상은 잠긴 것으로 본다(체인과 같은 방향).
    /// 읽기에 실패하면 마지막으로 읽은 값을 그대로 쓴다.
    /// </para>
    /// </remarks>
    public static class WorldOpenFront
    {
        /// <summary>
        /// 이 블록 수마다 노드에서 다시 읽는다. 확정은 남의 액션으로 일어나 렌더로 알 수 없다.
        /// </summary>
        public const int RefreshIntervalBlocks = 10;

        public static Address SheetAddress => Addresses.GetSheetAddress<WorldOpenSheet>();

        private static readonly WorldOpenCache Cache = new();

        /// <summary>캐시를 채운 에이전트. 바뀌면(플래닛 전환 등) 캐시를 비운다.</summary>
        private static IAgent _agent;

        /// <summary>렌더로 본 개방 시트 패치의 마지막 블록. tip 이 이보다 커진 상태로 읽어야 패치가 반영된다.</summary>
        private static long _patchBlockIndex = long.MinValue;

        private static bool _refreshRequired = true;
        private static bool _refreshing;

        /// <summary>어느 월드의 열린 끝이 바뀌면 메인 스레드에서 알린다. 구독자 예외는 다른 구독자에게 번지지 않는다.</summary>
        public static IObservable<Unit> Changed => Cache.Changed;

        /// <summary>
        /// 이 월드에서 지금 들어갈 수 있는 마지막 스테이지(아바타 진행과 무관한 전체 기준). 전부 잠겼으면
        /// <c>StageBegin - 1</c>. 월드 행이 없으면 게이트가 막을 수 없으므로 <see cref="int.MaxValue"/>.
        /// </summary>
        public static int GetOpenedStageEnd(int worldId)
        {
            var worldSheet = Game.Game.instance?.TableSheets?.WorldSheet;
            if (worldSheet is null || !worldSheet.TryGetValue(worldId, out var worldRow))
            {
                return int.MaxValue;
            }

            return Cache.GetOpenedStageEnd(worldRow);
        }

        /// <summary>이 월드의 이 스테이지에 지금 들어갈 수 있는지. 미미르·게이트 미만 스테이지는 항상 true.</summary>
        public static bool IsStageOpen(int worldId, int stageId) =>
            worldId == GameConfig.MimisbrunnrWorldId ||
            stageId < GameConfig.ProgressGatedStageBegin ||
            stageId <= GetOpenedStageEnd(worldId);

        /// <summary>
        /// 스테이지가 속한 월드를 <see cref="WorldSheet"/> 에서 찾아 <see cref="IsStageOpen(int, int)"/>. 월드에 속하지 않는
        /// 스테이지(이벤트 던전 등)는 게이트 대상이 아니라 true.
        /// </summary>
        public static bool IsStageOpen(int stageId)
        {
            if (stageId < GameConfig.ProgressGatedStageBegin)
            {
                return true;
            }

            var worldSheet = Game.Game.instance?.TableSheets?.WorldSheet;
            if (worldSheet is null)
            {
                return false;
            }

            return !worldSheet.TryGetByStageId(stageId, out var worldRow) ||
                IsStageOpen(worldRow.Id, stageId);
        }

        /// <summary>다음 블록 렌더에서 다시 읽게 한다(RPC 재접속).</summary>
        public static void RequestRefresh()
        {
            _refreshRequired = true;
        }

        /// <summary>
        /// 개방 시트 패치가 <paramref name="blockIndex"/> 에 들어갔다. tip 이 이 블록보다 커진 뒤(그 상태 루트에 패치가
        /// 반영된 뒤) 다시 읽는다 — 그 전 상태 루트로 읽고 요청을 끝내지 않게.
        /// </summary>
        public static void RequestRefreshAfter(long blockIndex)
        {
            _patchBlockIndex = Math.Max(_patchBlockIndex, blockIndex);
        }

        /// <summary>
        /// 플래닛·세션이 바뀌어 이전 값이 맞지 않을 때 비운다(게이트 구간은 다시 읽을 때까지 잠김).
        /// </summary>
        public static void Reset()
        {
            _agent = null;
            _patchBlockIndex = long.MinValue;
            _refreshRequired = true;
            Cache.Clear();
        }

        /// <summary>
        /// 블록 렌더마다 메인 스레드에서 부른다. 읽어 둔 상태로 tip + 1 에서 다시 판정하고, 아직 못 읽었거나, 요청이
        /// 있었거나, 패치가 반영된 tip 에 왔거나, <see cref="RefreshIntervalBlocks"/> 가 지났으면 노드에서 다시 읽는다.
        /// </summary>
        public static void OnBlockRendered(long tipIndex)
        {
            var game = Game.Game.instance;
            ResetIfAgentChanged(game?.Agent);
            if (Cache.State is not null && !Cache.Evaluate(tipIndex + 1, game?.TableSheets?.WorldSheet))
            {
                _refreshRequired = true; // 읽어 둔 상태로 판정하지 못했다. 다시 읽어 본다.
            }

            if (_refreshing)
            {
                return;
            }

            var patchPending = _patchBlockIndex >= Cache.FetchedTipIndex && tipIndex > _patchBlockIndex;
            if (_refreshRequired ||
                patchPending ||
                Cache.State is null ||
                tipIndex - Cache.FetchedTipIndex >= RefreshIntervalBlocks)
            {
                RefreshAsync().Forget();
            }
        }

        /// <summary>
        /// 노드의 tip 상태에서 개방 시트와 확정 블록을 읽어 다시 판정한다. 메인 스레드에서 부른다.
        /// </summary>
        /// <returns>
        /// 다시 읽었는지. 읽는 중이면(겹친 요청은 다음 블록에 한 번 더) 또는 실패하면 false — 실패해도 마지막 값은 남는다.
        /// </returns>
        public static async UniTask<bool> RefreshAsync()
        {
            var game = Game.Game.instance;
            var agent = game?.Agent;
            var worldSheet = game?.TableSheets?.WorldSheet;
            if (agent is null || worldSheet is null)
            {
                return false;
            }

            ResetIfAgentChanged(agent);
            if (_refreshing)
            {
                _refreshRequired = true;
                return false;
            }

            _refreshing = true;
            _refreshRequired = false;

            // 조회할 상태와 그 블록을 함께 잡는다 — 조회 중 tip 이 바뀌어도 둘이 어긋나지 않게.
            var tipIndex = agent.BlockIndex;
            var stateRootHash = agent.BlockTipStateRootHash;
            try
            {
                var fetched = await Cache.FetchAsync(
                    tipIndex,
                    () => FetchStateAsync(agent, stateRootHash),
                    worldSheet);
                if (!fetched)
                {
                    _refreshRequired = true; // 다음 블록에 다시 시도한다.
                }

                return fetched;
            }
            finally
            {
                _refreshing = false;
            }
        }

        /// <summary>
        /// 게이트가 읽는 값만 답하는 읽기 전용 상태. 나머지 키·계정은 null(없음)로 답한다 — 게이트는 그것을 미확정·
        /// 시트 없음으로 보아 잠그는 쪽으로 판정한다.
        /// </summary>
        /// <param name="sheetValue">레거시 계정의 <see cref="WorldOpenSheet"/> 값. 없으면 null.</param>
        /// <param name="confirmedBlocks"><see cref="Addresses.WorldOpen"/> 계정의 키 → 값. 없으면 null.</param>
        public static IWorldState CreateState(
            IValue sheetValue,
            IReadOnlyDictionary<Address, IValue> confirmedBlocks)
        {
            var legacy = new Dictionary<Address, IValue>();
            if (sheetValue is not null)
            {
                legacy[SheetAddress] = sheetValue;
            }

            return new SnapshotWorldState(new Dictionary<Address, IReadOnlyDictionary<Address, IValue>>
            {
                [ReservedAddresses.LegacyAccount] = legacy,
                [Addresses.WorldOpen] = confirmedBlocks ?? new Dictionary<Address, IValue>(),
            });
        }

        /// <summary>
        /// 읽은 적이 없을 때의 열린 끝 — 체인에 개방 시트가 없을 때와 같다. 미미르는 게이트 대상이 아니다.
        /// </summary>
        public static int GetUngatedStageEnd(WorldSheet.Row worldRow) =>
            worldRow.Id == GameConfig.MimisbrunnrWorldId
                ? worldRow.StageEnd
                : Math.Max(
                    worldRow.StageBegin - 1,
                    Math.Min(worldRow.StageEnd, GameConfig.ProgressGatedStageBegin - 1));

        private static void ResetIfAgentChanged(IAgent agent)
        {
            if (agent is null || ReferenceEquals(_agent, agent))
            {
                return;
            }

            if (_agent is not null)
            {
                Reset();
            }

            _agent = agent;
        }

        private static async Task<IWorldState> FetchStateAsync(IAgent agent, HashDigest<SHA256> stateRootHash)
        {
            var sheetValue = await agent.GetStateAsync(
                stateRootHash,
                ReservedAddresses.LegacyAccount,
                SheetAddress);
            var confirmedBlocks = new Dictionary<Address, IValue>();
            var state = CreateState(sheetValue, confirmedBlocks);
            if (state.TryGetPatchedSheet<WorldOpenSheet>(out var openSheet) && openSheet.Count > 0)
            {
                var keys = openSheet.Keys.Select(WorldOpenModule.OpenKey).ToList();
                var values = await agent.GetStateBulkAsync(stateRootHash, Addresses.WorldOpen, keys);
                foreach (var pair in values)
                {
                    confirmedBlocks[pair.Key] = pair.Value;
                }
            }

            return state;
        }

        private sealed class SnapshotWorldState : IWorldState
        {
            private readonly IReadOnlyDictionary<Address, IReadOnlyDictionary<Address, IValue>> _accounts;

            public SnapshotWorldState(
                IReadOnlyDictionary<Address, IReadOnlyDictionary<Address, IValue>> accounts)
            {
                _accounts = accounts;
            }

            public ITrie Trie => throw new NotSupportedException();

            public bool Legacy => false;

            public int Version => BlockMetadata.CurrentProtocolVersion;

            public IAccountState GetAccountState(Address address) =>
                new SnapshotAccountState(
                    _accounts.TryGetValue(address, out var states)
                        ? states
                        : new Dictionary<Address, IValue>());
        }

        private sealed class SnapshotAccountState : IAccountState
        {
            private readonly IReadOnlyDictionary<Address, IValue> _states;

            public SnapshotAccountState(IReadOnlyDictionary<Address, IValue> states)
            {
                _states = states;
            }

            public ITrie Trie => throw new NotSupportedException();

            public IValue GetState(Address address) =>
                _states.TryGetValue(address, out var value) ? value : null;

            public IReadOnlyList<IValue> GetStates(IReadOnlyList<Address> addresses) =>
                addresses.Select(GetState).ToList();
        }
    }

    /// <summary>
    /// <see cref="WorldOpenFront"/> 의 캐시. 마지막으로 읽은 상태와 그 상태로 계산한 월드별 열린 끝을 들고 있다.
    /// </summary>
    public sealed class WorldOpenCache
    {
        private readonly Subject<Unit> _changed = new();

        /// <summary>월드 id → 들어갈 수 있는 마지막 스테이지. null 이면 한 번도 못 읽었다.</summary>
        private Dictionary<int, int> _openedStageEnds;

        /// <summary><see cref="Clear"/> 마다 올린다. 그 전에 시작한 조회 결과는 버린다.</summary>
        private int _generation;

        /// <summary>실패 로그를 다시 남기기까지의 블록 수.</summary>
        public const int FailureLogIntervalBlocks = 100;

        /// <summary>마지막으로 실패 로그를 남긴 블록. 성공하면 null.</summary>
        private long? _lastFailureLoggedTipIndex;

        /// <summary>마지막으로 읽은 상태. 한 번도 못 읽었으면 null.</summary>
        public IWorldState State { get; private set; }

        /// <summary><see cref="State"/> 를 읽은 tip.</summary>
        public long FetchedTipIndex { get; private set; } = long.MinValue;

        /// <summary>어느 월드의 열린 끝이 바뀌면 알린다. 구독자 예외는 로그만 남기고 삼킨다.</summary>
        public IObservable<Unit> Changed => new GuardedObservable(_changed);

        /// <summary>이 월드의 열린 끝. 아직 계산하지 못했으면 시트 없음과 같게 판정한다.</summary>
        public int GetOpenedStageEnd(WorldSheet.Row worldRow) =>
            _openedStageEnds is not null && _openedStageEnds.TryGetValue(worldRow.Id, out var end)
                ? end
                : WorldOpenFront.GetUngatedStageEnd(worldRow);

        /// <summary>
        /// <paramref name="fetch"/> 로 상태를 읽어 <c>tipIndex + 1</c> 에서 판정한다. 읽기나 판정에 실패하면 마지막으로 읽은
        /// 상태와 판정을 그대로 둔다.
        /// </summary>
        /// <returns>읽어서 반영했는지.</returns>
        public async Task<bool> FetchAsync(long tipIndex, Func<Task<IWorldState>> fetch, WorldSheet worldSheet)
        {
            var generation = _generation;
            IWorldState state;
            try
            {
                state = await fetch();
            }
            catch (Exception e)
            {
                LogFailure(tipIndex, "read", e);
                return false;
            }

            if (generation != _generation)
            {
                return false; // 조회 중에 비워졌다(플래닛 전환 등).
            }

            if (!TryCompute(state, tipIndex + 1, worldSheet, out var ends))
            {
                return false; // 새 상태로 판정하지 못했다. 이전 상태를 그대로 쓴다.
            }

            State = state;
            FetchedTipIndex = tipIndex;
            _lastFailureLoggedTipIndex = null;
            Apply(ends);
            return true;
        }

        /// <summary>읽어 둔 상태로 <paramref name="blockIndex"/> 에서 다시 판정한다. 노드는 조회하지 않는다.</summary>
        /// <returns>판정했는지. 읽은 상태가 없거나 판정에 실패하면 false(이전 판정 유지).</returns>
        public bool Evaluate(long blockIndex, WorldSheet worldSheet)
        {
            if (State is null || !TryCompute(State, blockIndex, worldSheet, out var ends))
            {
                return false;
            }

            Apply(ends);
            return true;
        }

        /// <summary>읽은 상태와 판정을 비운다. 게이트 구간은 다시 읽을 때까지 잠긴다.</summary>
        public void Clear()
        {
            _generation++;
            State = null;
            FetchedTipIndex = long.MinValue;
            _lastFailureLoggedTipIndex = null;
            Apply(null);
        }

        private bool TryCompute(
            IWorldState state,
            long blockIndex,
            WorldSheet worldSheet,
            out Dictionary<int, int> ends)
        {
            ends = null;
            if (worldSheet is null)
            {
                return false;
            }

            try
            {
                ends = worldSheet.OrderedList.ToDictionary(
                    row => row.Id,
                    row => WorldOpenGate.GetOpenedStageEnd(state, row, blockIndex));
                return true;
            }
            catch (Exception e)
            {
                LogFailure(blockIndex, "evaluate", e);
                ends = null;
                return false;
            }
        }

        /// <summary>
        /// 실패 로그. 노드가 계속 응답하지 않으면 매 블록 실패하므로, 성공 뒤 첫 실패와 그 뒤
        /// <see cref="FailureLogIntervalBlocks"/> 마다만 남긴다.
        /// </summary>
        private void LogFailure(long blockIndex, string what, Exception e)
        {
            if (_lastFailureLoggedTipIndex is { } last && blockIndex - last < FailureLogIntervalBlocks)
            {
                return;
            }

            _lastFailureLoggedTipIndex = blockIndex;
            NcDebug.LogWarning($"[WorldOpen] failed to {what} world open state at {blockIndex}; keeping the last one. {e}");
        }

        private void Apply(Dictionary<int, int> ends)
        {
            var previous = _openedStageEnds;
            _openedStageEnds = ends;
            var changed = previous is null || ends is null
                ? !ReferenceEquals(previous, ends)
                : previous.Count != ends.Count ||
                  ends.Any(pair => !previous.TryGetValue(pair.Key, out var end) || end != pair.Value);
            if (!changed)
            {
                return;
            }

            try
            {
                _changed.OnNext(Unit.Default);
            }
            catch (Exception e)
            {
                NcDebug.LogError($"[WorldOpen] a {nameof(WorldOpenFront.Changed)} subscriber threw: {e}");
            }
        }

        /// <summary>구독자마다 예외를 막아 한 구독자의 예외가 캐시·다른 구독자에 번지지 않게 한다.</summary>
        private sealed class GuardedObservable : IObservable<Unit>
        {
            private readonly IObservable<Unit> _source;

            public GuardedObservable(IObservable<Unit> source)
            {
                _source = source;
            }

            public IDisposable Subscribe(IObserver<Unit> observer) =>
                UniRx.ObservableExtensions.Subscribe(_source, _ =>
                {
                    try
                    {
                        observer.OnNext(Unit.Default);
                    }
                    catch (Exception e)
                    {
                        NcDebug.LogError($"[WorldOpen] a {nameof(WorldOpenFront.Changed)} subscriber threw: {e}");
                    }
                });
        }
    }
}
