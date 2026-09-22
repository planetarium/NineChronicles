namespace Nekoyume.IAPStore
{
    /// <summary>
    /// 상점 UI 가 보는 상품 정보. 어느 스토어에서 왔는지 감춘다.
    /// </summary>
    /// <remarks>
    /// UI 가 <c>UnityEngine.Purchasing.Product</c> 를 직접 보면 원스토어 경로를 붙일 수 없다.
    /// 원스토어에서는 그 타입을 만들 수 없기 때문이다 — <c>Product</c> 의 생성자가 <c>internal</c> 이고
    /// Unity IAP 는 별도 어셈블리(<c>Unity.Purchasing</c>)라 <c>Assembly-CSharp</c> 에서 접근할 수 없다.
    ///
    /// UI 가 상품에서 실제로 읽는 것은 "id 로 찾아 현지 가격을 표시한다"가 전부라, 그만큼만 담는다.
    /// </remarks>
    public sealed class IapProductInfo
    {
        /// <summary>스토어에 등록된 상품 ID. Google Play 든 원스토어든 같은 SKU 문자열을 쓴다.</summary>
        public string Id { get; }

        /// <summary>현지화된 상품명. 지금은 로그에만 쓴다.</summary>
        public string Title { get; }

        /// <summary>ISO 통화 코드. 가격 표기를 만들 때 쓴다.</summary>
        public string CurrencyCode { get; }

        /// <summary>스토어가 만들어 준 가격 문자열. 지금은 로그에만 쓴다.</summary>
        public string PriceString { get; }

        /// <summary>현지 통화 가격. 할인 전 원가 계산에 쓰므로 문자열이 아니라 수치여야 한다.</summary>
        public decimal Price { get; }

        public IapProductInfo(
            string id,
            string title,
            string currencyCode,
            string priceString,
            decimal price)
        {
            Id = id;
            Title = title;
            CurrencyCode = currencyCode;
            PriceString = priceString;
            Price = price;
        }
    }
}
