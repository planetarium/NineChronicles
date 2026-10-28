using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Libplanet.Types.Blocks;
using Nekoyume.Action;
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
    /// 판정 블록은 tip + 1 이다 — 지금 보낸 tx 는 빨라야 다음 블록에 들어간다. 아직 못 읽었거나 읽기에 실패하면
    /// <see cref="GameConfig.ProgressGatedStageBegin"/> 이상은 잠긴 것으로 본다(체인과 같은 방향).
    /// </para>
    /// </remarks>
    public static class WorldOpenFront
    {
        /// <summary>
        /// 이 블록 수마다 다시 읽는다. 확정·열림은 남의 액션·블록 경과로 일어나 렌더로 알 수 없다.
        /// </summary>
        public const int RefreshIntervalBlocks = 10;

        public static Address SheetAddress => Addresses.GetSheetAddress<WorldOpenSheet>();

        private static readonly Subject<Unit> ChangedSubject = new();

        /// <summary>월드 id → 들어갈 수 있는 마지막 스테이지. null 이면 아직 못 읽었다.</summary>
        private static Dictionary<int, int> _openedStageEnds;

        private static long _refreshedBlockIndex = long.MinValue;
        private static bool _refreshRequired = true;
        private static bool _refreshing;

        /// <summary>갱신으로 어느 월드의 열린 끝이 바뀌면 메인 스레드에서 알린다.</summary>
        public static IObservable<Unit> Changed => ChangedSubject;

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

            if (_openedStageEnds is not null && _openedStageEnds.TryGetValue(worldId, out var end))
            {
                return end;
            }

            // 아직 못 읽었거나 읽은 뒤 시트에 들어온 월드다. 체인에 개방 시트가 없을 때와 같게 판정한다.
            return WorldOpenGate.GetOpenedStageEnd(CreateState(null, null), worldRow, 0);
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

        /// <summary>다음 블록 렌더에서 다시 읽게 한다(개방 시트 패치, RPC 재접속).</summary>
        public static void RequestRefresh()
        {
            _refreshRequired = true;
        }

        /// <summary>
        /// 블록 렌더마다 메인 스레드에서 부른다. 아직 못 읽었거나, 요청이 있었거나, <see cref="RefreshIntervalBlocks"/>
        /// 가 지났으면 다시 읽는다.
        /// </summary>
        public static void OnBlockRendered(long blockIndex)
        {
            if (_refreshing)
            {
                return;
            }

            if (_refreshRequired ||
                _openedStageEnds is null ||
                blockIndex - _refreshedBlockIndex >= RefreshIntervalBlocks)
            {
                RefreshAsync().Forget();
            }
        }

        /// <summary>
        /// 노드의 tip 상태에서 개방 시트와 확정 블록을 읽어 월드별 열린 끝을 다시 계산한다. 메인 스레드에서 부른다.
        /// </summary>
        /// <returns>
        /// 다시 계산했는지. 읽는 중이면(겹친 요청은 끝난 뒤 다음 블록에 한 번 더) 또는 실패하면 false.
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

                var blockIndex = tipIndex + 1;
                var ends = new Dictionary<int, int>();
                foreach (var worldRow in worldSheet.OrderedList)
                {
                    ends[worldRow.Id] = WorldOpenGate.GetOpenedStageEnd(state, worldRow, blockIndex);
                }

                Apply(ends);
                _refreshedBlockIndex = tipIndex;
                return true;
            }
            catch (Exception e)
            {
                NcDebug.LogWarning($"[WorldOpen] failed to refresh world open state: {e}");
                // 못 읽으면 게이트 구간은 잠근다. 다음 블록에 다시 시도한다.
                Apply(null);
                _refreshRequired = true;
                return false;
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

        private static void Apply(Dictionary<int, int> ends)
        {
            var previous = _openedStageEnds;
            _openedStageEnds = ends;
            var changed = previous is null || ends is null
                ? !ReferenceEquals(previous, ends)
                : previous.Count != ends.Count ||
                  ends.Any(pair => !previous.TryGetValue(pair.Key, out var end) || end != pair.Value);
            if (changed)
            {
                ChangedSubject.OnNext(Unit.Default);
            }
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
}
