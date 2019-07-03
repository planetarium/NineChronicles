using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using UniRx;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI
{
    public abstract class TooltipWidget<T> : Widget where T : Model.Tooltip
    {
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
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
            Model.anchorPresetType.Subscribe(SubscribeAnchorPresetType).AddTo(_disposablesForModel);
            Model.offset.Subscribe(SubscribeOffset).AddTo(_disposablesForModel);
        }
        
        public override void Close()
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = null;
            
            base.Close();
        }
        
        private void SubscribeTarget(RectTransform value)
        {
            
        }

        private void SubscribeAnchorPresetType(AnchorPresetType value)
        {
            
        }

        private void SubscribeOffset(float3 value)
        {
            
        }
    }
}
