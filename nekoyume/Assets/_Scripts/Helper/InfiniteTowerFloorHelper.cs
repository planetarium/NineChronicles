using System.Linq;
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

        /// <summary>깬 층 행 번호(<c>ClearedFloor</c>)의 화면 층. 아직 못 깼으면 0.</summary>
        public static int GetClearedFloorNumber(InfiniteTowerFloorSheet floorSheet, int clearedFloorId)
        {
            return clearedFloorId <= 0 ? 0 : GetFloorNumber(floorSheet, clearedFloorId);
        }
    }
}
