using Nekoyume.EnumType;
using UniRx;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class Tooltip
    {
        public const AnchorPresetType DefaultAnchorPresetType = AnchorPresetType.TopLeft;
        public static readonly float3 DefaultOffset = new float3(10f, 0f, 0f);
        
        public readonly ReactiveProperty<RectTransform> target = new ReactiveProperty<RectTransform>();
        public readonly ReactiveProperty<AnchorPresetType> anchorPresetType = new ReactiveProperty<AnchorPresetType>(DefaultAnchorPresetType);
        public readonly ReactiveProperty<float3> offset = new ReactiveProperty<float3>(DefaultOffset);
    }
}
