using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GeneratedApiNamespace.InAppPurchaseServiceClient;

namespace Nekoyume.UI.Module
{
    /// <summary>
    /// 상품 응답의 복권 티켓(<see cref="VoucherTicketSchema"/>) 표시 규칙.
    /// 상품 셀과 상세 팝업이 **같은 순서·같은 아이콘**을 쓰도록 여기 모아 둔다.
    /// 포탈 웹샵(9c-portal `voucherTicketUtils.ts`)과 규칙을 맞춘 것이다.
    /// </summary>
    public static class VoucherTicketPresenter
    {
        /// <summary>
        /// 티켓 종류는 포탈 정책(prizeTables 키)이 정하는 **열린 집합**이다.
        /// 그래서 알려진 등급만 순서를 고정하고, 모르는 종류는 뒤에 알파벳순으로 붙인다 —
        /// 새 등급이 추가돼도 화면에서 사라지면 안 되므로 랭크 미등록을 제외 조건으로 쓰지 않는다.
        /// </summary>
        private static readonly Dictionary<string, int> TypeRank = new()
        {
            { "LEGEND", 0 },
            { "PREMIUM", 1 },
            { "STANDARD", 2 },
        };

        /// <summary>종류별 아이콘 파일명. 모르는 종류는 무늬 없는 기본 티켓으로 떨어뜨린다.</summary>
        private static readonly Dictionary<string, string> TypeIcon = new()
        {
            { "PREMIUM", "voucher_premium" },
            { "STANDARD", "voucher_standard" },
        };

        public static List<VoucherTicketSchema> Sort(IEnumerable<VoucherTicketSchema> tickets)
        {
            if (tickets == null)
            {
                return new List<VoucherTicketSchema>();
            }

            return tickets
                .Where(t => t != null && t.Count > 0)
                .OrderBy(t => TypeRank.TryGetValue(t.TicketType, out var r) ? r : int.MaxValue)
                .ThenBy(t => t.TicketType, System.StringComparer.Ordinal)
                .ToList();
        }

        /// <param name="small">true = 상품 셀(64px), false = 상세 팝업(256px)</param>
        public static Sprite LoadIcon(string ticketType, bool small)
        {
            var name = ticketType != null && TypeIcon.TryGetValue(ticketType, out var n)
                ? n
                : "voucher_icon";
            return Resources.Load<Sprite>($"UI/Textures/Voucher/{name}_{(small ? 64 : 256)}");
        }
    }
}
