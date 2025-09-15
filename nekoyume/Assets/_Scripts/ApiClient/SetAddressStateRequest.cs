using System;

namespace Nekoyume.ApiClient
{
    [Serializable]
    public class SetAddressStateRequest
    {
        public string Address { get; set; } = string.Empty;
        public string AccountAddress { get; set; } = string.Empty;
        public string PlanetId { get; set; } = string.Empty;
        public string TargetAddress { get; set; } = string.Empty;
        public string? TargetAddress2 { get; set; }
        public string? TargetAddress3 { get; set; }

        // 디지털 서명 관련 필드 (필수)
        public string PublicKey { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public long Timestamp { get; set; }
    }
}
