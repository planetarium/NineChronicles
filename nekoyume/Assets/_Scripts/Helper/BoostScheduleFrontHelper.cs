using System;
using System.Security.Cryptography;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Common;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    /// <summary>
    /// 보상 미리보기(보상 정보·준비 화면)용 BoostScheduleSheet 조회와, 결과 재현·세션 중 갱신에 쓸 시트 확보.
    /// </summary>
    /// <remarks>
    /// 미리보기는 "지금 행동하면 받을 양"을 보여주려고 다음 블록(<see cref="NextBlockIndex"/>)으로 찾는다 — 지금 보낸
    /// tx 는 빨라야 다음 블록에 들어간다. 결과 재현은 액션 블록(<c>eval.BlockIndex</c>)과
    /// <see cref="GetSheetForAction"/> 이 돌려준 시트로 찾는다.
    /// </remarks>
    public static class BoostScheduleFrontHelper
    {
        /// <summary>
        /// 지금 보낸 tx 가 포함될 수 있다고 보는 블록 수(다음 블록부터). 보내기 직전 확인은 이 구간 안에서 행이
        /// 켜지거나 꺼지는지 본다 — 경계 직전에 보내면 포함되는 블록에 따라 비용·필요 수량이 달라 실패한다.
        /// </summary>
        public const int InclusionLookaheadBlocks = 3;

        public static Libplanet.Crypto.Address SheetAddress =>
            Addresses.TableSheet.Derive(nameof(BoostScheduleSheet));

        /// <summary>지금 보낸 tx 가 들어갈 수 있는 가장 이른 블록(tip + 1). 에이전트가 없으면 0.</summary>
        public static long NextBlockIndex => (Game.Game.instance?.Agent?.BlockIndex ?? -1) + 1;

        /// <summary>다음 블록에 이 target·id 로 걸린 행. 없으면 null.</summary>
        public static BoostScheduleSheet.Row FindActiveNow(string target, int targetId)
        {
            var game = Game.Game.instance;
            if (game?.TableSheets?.BoostScheduleSheet is null || game.Agent is null)
            {
                return null;
            }

            return game.TableSheets.BoostScheduleSheet.FindActive(target, targetId, NextBlockIndex);
        }

        /// <summary>
        /// 다음 블록부터 <see cref="InclusionLookaheadBlocks"/> 블록 안에서 이 target·id 에 걸리는 행이 바뀌는지
        /// (이벤트가 시작되거나 끝나는지). 그렇다면 지금 보낸 tx 는 포함되는 블록에 따라 다른 값으로 처리된다.
        /// </summary>
        public static bool IsBoundaryAhead(string target, int targetId)
        {
            var sheet = Game.Game.instance?.TableSheets?.BoostScheduleSheet;
            if (sheet is null)
            {
                return false;
            }

            var next = NextBlockIndex;
            var first = sheet.FindActive(target, targetId, next);
            for (var offset = 1; offset < InclusionLookaheadBlocks; offset++)
            {
                if (!ReferenceEquals(first, sheet.FindActive(target, targetId, next + offset)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 망치 1개가 다음 블록에서 주는 exp. 액션(<c>ItemEnhancement</c>)과 같은 계산이다 — 원본을 쓰면 이벤트 중
        /// 목표 레벨·NCG 비용을 낮게 보여, 표시된 비용만큼만 NCG 를 가진 아바타의 강화가 실패하고 최대 레벨에
        /// 필요한 망치 수도 많게 잡힌다.
        /// </summary>
        public static long GetHammerExp(EnhancementCostSheetV3 enhancementCostSheet, int hammerId) =>
            Nekoyume.Action.ItemEnhancement.GetHammerExp(
                enhancementCostSheet,
                hammerId,
                FindActiveNow(BoostScheduleSheet.Targets.EnhancementHammerExp, hammerId));

        /// <summary>
        /// 이 액션이 실행될 때 실제로 본 BoostScheduleSheet. 결과 재현은 지금 클라 시트가 아니라 이 시트로 해야 한다.
        /// </summary>
        /// <param name="blockIndex">액션이 들어간 블록(<c>eval.BlockIndex</c>).</param>
        /// <param name="previousState">액션 직전 상태 루트(<c>eval.PreviousState</c>).</param>
        /// <returns>시트. 클라 시트가 아직 없으면 null(부스트 없음으로 재현).</returns>
        /// <remarks>
        /// <para>
        /// 패치 렌더는 결과 렌더보다 먼저 반영된다(결과 렌더는 스레드풀에서 상태를 읽은 뒤 그린다). 그래서 패치 블록에서
        /// 패치보다 앞서 실행된 tx 의 결과창을 지금 시트로 그리면 지급과 다른 수량을 보여준다. 지금 시트가 들어온
        /// 블록보다 뒤의 액션은 지금 시트를 그대로 쓰고, 그 블록 이하의 액션(패치 블록·재조회 구간)만 액션 직전
        /// 상태에서 시트를 읽는다. 패치는 드물어 추가 조회도 드물다.
        /// </para>
        /// <para>노드 조회가 블로킹이므로 스레드풀에서만 부른다.</para>
        /// </remarks>
        public static BoostScheduleSheet GetSheetForAction(long blockIndex, HashDigest<SHA256> previousState)
        {
            var tableSheets = Game.Game.instance?.TableSheets;
            if (tableSheets is null)
            {
                return null;
            }

            var current = tableSheets.GetBoostScheduleSheet(out var changedAt);
            if (blockIndex > changedAt)
            {
                return current;
            }

            if (PlayerLoopHelper.IsMainThread)
            {
                // 아래 조회는 블로킹이라 메인 스레드에서 부르면 멈춘다. 잘못된 호출이므로 지금 시트로 버틴다.
                NcDebug.LogWarning($"[BoostSchedule] {nameof(GetSheetForAction)} called on the main thread.");
                return current;
            }

            try
            {
                var value = StateGetter.GetState(previousState, ReservedAddresses.LegacyAccount, SheetAddress);
                var sheet = new BoostScheduleSheet();
                if (value is Text text)
                {
                    sheet.Set(text.Value);
                }

                // 체인에 시트가 없으면(패치된 적 없음) 액션도 부스트 없이 처리했다 — 빈 시트가 그와 같다.
                return sheet;
            }
            catch (Exception e)
            {
                NcDebug.LogWarning(
                    $"[BoostSchedule] failed to read the sheet the action at {blockIndex} saw;" +
                    $" falling back to the current one. {e}");
                return current;
            }
        }

        /// <summary>
        /// 노드의 지금 tip 상태에서 BoostScheduleSheet 를 다시 받아 클라 시트를 맞춘다. 세션 중 갱신은 패치 렌더에
        /// 기대는데, RPC 재접속 사이나 부팅 중(시트를 읽은 뒤 대입 전)에 지나간 패치는 렌더를 받지 못한다.
        /// </summary>
        /// <returns>시트를 확인했는지(바뀌지 않았어도 true). 실패하면 false — 호출부가 다음에 다시 시도한다.</returns>
        public static async UniTask<bool> RefreshFromNodeAsync()
        {
            var game = Game.Game.instance;
            var agent = game?.Agent;
            var tableSheets = game?.TableSheets;
            if (agent is null || tableSheets is null)
            {
                return false;
            }

            // 조회할 상태와 그 블록을 함께 잡는다 — 조회 중 tip 이 바뀌어도 둘이 어긋나지 않게. tip 의
            // StateRootHash 는 tip 블록의 tx 를 실행하기 전(= tip - 1 까지) 상태라 tip - 1 로 기록한다. tip 으로 기록하면
            // tip 블록에서 방금 렌더로 적용한 패치를 그 전 값으로 되돌린다.
            var generation = tableSheets.BoostScheduleSheetVerifyGeneration;
            var blockIndex = agent.BlockIndex - 1;
            var stateRootHash = agent.BlockTipStateRootHash;
            try
            {
                var value = await agent.GetStateAsync(stateRootHash, ReservedAddresses.LegacyAccount, SheetAddress);
                if (value is not Text text)
                {
                    // 체인에 시트가 없다 = 패치된 적이 없다. 부팅 때 쓴 번들 시트가 그 상태와 같다.
                    tableSheets.MarkBoostScheduleSheetVerified(generation);
                    return true;
                }

                if (tableSheets.ReplaceBoostScheduleSheet(text.Value, blockIndex))
                {
                    NcDebug.Log($"[BoostSchedule] BoostScheduleSheet refreshed from the node at {blockIndex}.");
                }

                tableSheets.MarkBoostScheduleSheetVerified(generation);
                return true;
            }
            catch (Exception e)
            {
                NcDebug.LogWarning($"[BoostSchedule] failed to refresh BoostScheduleSheet from the node: {e}");
                return false;
            }
        }
    }
}
