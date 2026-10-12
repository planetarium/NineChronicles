using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    /// <summary>
    /// 보상 미리보기(보상 정보·준비 화면)용 BoostScheduleSheet 조회. 결과 재현은 액션 블록(<c>eval.BlockIndex</c>)으로
    /// 직접 찾고, 여기는 "지금 행동하면 받을 양"을 보여주려고 현재 블록으로 찾는다.
    /// </summary>
    public static class BoostScheduleFrontHelper
    {
        /// <summary>지금 블록에 이 target·id 로 걸린 행. 없으면 null.</summary>
        public static BoostScheduleSheet.Row FindActiveNow(string target, int targetId)
        {
            var game = Game.Game.instance;
            if (game?.TableSheets?.BoostScheduleSheet is null || game.Agent is null)
            {
                return null;
            }

            return game.TableSheets.BoostScheduleSheet.FindActive(target, targetId, game.Agent.BlockIndex);
        }

        /// <summary>
        /// 망치 1개가 지금 블록에서 주는 exp. 액션(<c>ItemEnhancement</c>)과 같은 계산이다 — 원본을 쓰면 이벤트 중
        /// 목표 레벨·NCG 비용을 낮게 보여, 표시된 비용만큼만 NCG 를 가진 아바타의 강화가 실패하고 최대 레벨에
        /// 필요한 망치 수도 많게 잡힌다.
        /// </summary>
        public static long GetHammerExp(EnhancementCostSheetV3 enhancementCostSheet, int hammerId) =>
            Nekoyume.Action.ItemEnhancement.GetHammerExp(
                enhancementCostSheet,
                hammerId,
                FindActiveNow(BoostScheduleSheet.Targets.EnhancementHammerExp, hammerId));
    }
}
