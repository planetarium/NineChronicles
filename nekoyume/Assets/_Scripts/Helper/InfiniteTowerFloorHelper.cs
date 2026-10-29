using System.Linq;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    /// <summary>
    /// 무한의탑 층 행 번호(<c>Id</c>)와 화면에 보이는 층(<c>Floor</c>) 사이 변환.
    /// </summary>
    /// <remarks>
    /// 시즌마다 층 행을 <c>FloorBegin</c>~<c>FloorEnd</c> 번호 대역에 따로 두면 다음 시즌 층을 미리 넣어 둘 수 있다.
    /// 그래서 층 번호로 행을 찾을 때는 현재 시즌 대역 안에서만 찾고, 체인이 쓰는 행 번호(액션 <c>FloorId</c>,
    /// <c>InfiniteTowerInfo.ClearedFloor</c>)는 보여줄 때 <c>Floor</c> 로 바꾼다. 대역이 없는 시즌(1~120)은 둘이 같다.
    /// 다음 시즌 층을 미리 넣을 때는 지금 시즌 일정 행에도 대역(예: 1~120)을 함께 넣어야 한다 — 비워 두면
    /// 범위가 1~int.MaxValue 라 같은 층 번호의 다른 시즌 행이 섞이고, 체인도 지금 시즌에 다음 시즌 층을 받는다.
    /// </remarks>
    public static class InfiniteTowerFloorHelper
    {
        /// <summary>현재 시즌 대역 안에서 화면 층 <paramref name="floor"/> 의 행. 없으면 null.</summary>
        public static InfiniteTowerFloorSheet.Row FindFloorRow(
            InfiniteTowerFloorSheet floorSheet,
            InfiniteTowerScheduleSheet.Row scheduleRow,
            int floor)
        {
            return floorSheet?.Values.FirstOrDefault(row =>
                row.Floor == floor &&
                (scheduleRow is null || (row.Id >= scheduleRow.FloorBegin && row.Id <= scheduleRow.FloorEnd)));
        }

        /// <summary>층 행 번호의 화면 층. 행이 없으면 행 번호를 그대로 돌려준다.</summary>
        public static int GetFloorNumber(InfiniteTowerFloorSheet floorSheet, int floorId)
        {
            return floorSheet != null && floorSheet.TryGetValue(floorId, out var row) ? row.Floor : floorId;
        }

        /// <summary>
        /// 현재 시즌에서 깬 층(<c>ClearedFloor</c>)의 화면 층. 아직 못 깼으면 0.
        /// 시즌이 막 바뀌어 지난 시즌 정보가 남아 있으면(다른 탑 번호이거나 대역 밖) 0 으로 본다 — 체인은 새 시즌 첫 전투에서 초기화한다.
        /// </summary>
        public static int GetClearedFloorNumber(
            InfiniteTowerFloorSheet floorSheet,
            InfiniteTowerScheduleSheet.Row scheduleRow,
            InfiniteTowerInfo infiniteTowerInfo)
        {
            var clearedFloorId = infiniteTowerInfo?.ClearedFloor ?? 0;
            if (clearedFloorId <= 0)
            {
                return 0;
            }

            if (scheduleRow != null &&
                (infiniteTowerInfo.InfiniteTowerId != scheduleRow.InfiniteTowerId ||
                 clearedFloorId < scheduleRow.FloorBegin ||
                 clearedFloorId > scheduleRow.FloorEnd))
            {
                return 0;
            }

            return GetFloorNumber(floorSheet, clearedFloorId);
        }
    }
}
