using Nekoyume.EnumType;
using UniRx;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class Tooltip
    {
        public readonly ReactiveProperty<RectTransform> target = new ReactiveProperty<RectTransform>();
    }
}
