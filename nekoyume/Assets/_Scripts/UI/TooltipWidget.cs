using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Game;
using UniRx;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI
{
    public abstract class TooltipWidget<T> : Widget where T : Model.Tooltip
    {
        public static readonly float3 DefaultOffset = new float3(10f, 0f, 0f);
        
        public RectTransform panel;
    
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();
        
        public override WidgetType WidgetType => WidgetType.Tooltip;
        
        public T Model { get; private set; }
        
        public void Show(T value)
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = value;
            
            Show();
        }

        public override void Show()
        {
            base.Show();
            
            if (Model is null)
            {
                Close();
                
                return;
            }

            Model.target.Subscribe(SubscribeTarget).AddTo(_disposablesForModel);
        }
        
        public override void Close()
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = null;
            
            base.Close();
        }
        
        private void SubscribeTarget(RectTransform target)
        {
            var position = target.position;
            var targetRect = target.rect;
            var tooltipRect = RectTransform.rect;
            position.x += (targetRect.x + tooltipRect.size.x) * 0.5f;
            position.y -= (targetRect.y + tooltipRect.size.y) * 0.5f;
            RectTransform.position = position;
            var anchoredPosition = RectTransform.anchoredPosition; 
            anchoredPosition.x += DefaultOffset.x;
            anchoredPosition.y += DefaultOffset.y;
            RectTransform.anchoredPosition = anchoredPosition;
            RectTransform.MoveInsideOfScreen(ActionCamera.instance.Cam, PivotPresetType.BottomRight);
        }
    }
}
