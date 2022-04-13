using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public abstract class NewTooltipWidget : Widget
    {
        public static readonly float2 DefaultOffsetFromTarget = new float2(10f, 0f);
        public static readonly float2 DefaultMarginFromParent = new float2(10f, 10f);

        public RectTransform panel;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public override WidgetType WidgetType => WidgetType.Tooltip;

        protected abstract PivotPresetType TargetPivotPresetType { get; }
        protected virtual float2 OffsetFromTarget => DefaultOffsetFromTarget;
        protected virtual float2 MarginFromParent => DefaultMarginFromParent;

        protected virtual void UpdateAnchoredPosition(RectTransform target)
        {
            panel.MoveToRelatedPosition(target, TargetPivotPresetType, OffsetFromTarget);
            panel.MoveInsideOfParent(MarginFromParent);
        }
    }
}
