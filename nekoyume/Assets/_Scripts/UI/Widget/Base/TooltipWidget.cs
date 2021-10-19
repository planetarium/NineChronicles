using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public abstract class TooltipWidget<T> : Widget where T : Model.Tooltip
    {
        public static readonly float2 DefaultOffsetFromTarget = new float2(10f, 0f);
        public static readonly float2 DefaultMarginFromParent = new float2(10f, 10f);

        public RectTransform panel;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public override WidgetType WidgetType => WidgetType.Tooltip;

        public T Model { get; private set; }

        protected abstract PivotPresetType TargetPivotPresetType { get; }
        protected virtual float2 OffsetFromTarget => DefaultOffsetFromTarget;
        protected virtual float2 MarginFromParent => DefaultMarginFromParent;

        public void Show(T value)
        {
            if (value is null)
            {
                Close();

                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            Model = value;
            Model.target.Subscribe(SubscribeTarget).AddTo(_disposablesForModel);

            Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = null;

            base.Close(ignoreCloseAnimation);
        }

        protected virtual void SubscribeTarget(RectTransform target)
        {
            panel.MoveToRelatedPosition(target, TargetPivotPresetType, OffsetFromTarget);
            UpdateAnchoredPosition();
        }

        protected virtual void UpdateAnchoredPosition()
        {
            panel.MoveInsideOfParent(MarginFromParent);
        }
    }
}
