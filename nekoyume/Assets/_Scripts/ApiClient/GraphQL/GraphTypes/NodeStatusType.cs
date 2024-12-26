#nullable enable

using System.Text.Json.Serialization;

namespace Nekoyume.GraphQL.GraphTypes
{
    // TODO: 필요한 파라메터 추가 구현 필요
    public class NodeStatusType
    {
        public class InnerType
        {
            [JsonPropertyName("tip")]
            public TipType? Tip;

            public override string ToString()
            {
                return $"Tip: {Tip}";
            }
        }

        [JsonPropertyName("nodeStatus")]
        public InnerType? NodeStatus;

        [JsonPropertyName("isMining")]
        public bool? IsMining;

        [JsonPropertyName("informationalVersion")]
        public string? InformationalVersion;

        [JsonPropertyName("stagedTxIdsCount")]
        public int? StagedTxIdsCount;

        public override string ToString()
        {
            var nodeStatus = NodeStatus is null ? "null" : NodeStatus.ToString();
            return $"NodeStatus: {{ {nodeStatus} }}, IsMining: {IsMining}, InformationalVersion: {InformationalVersion}, StagedTxIdsCount: {StagedTxIdsCount}";
        }
    }

    public class NodeStatusResponse
    {
        public TipResultQuery NodeStatus;
    }

    public class TipResultQuery
    {
        public TipResult Tip;
    }

    public class TipResult
    {
        public string Id;
    }
}
