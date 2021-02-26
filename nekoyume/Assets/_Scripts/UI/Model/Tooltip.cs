using System;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class Tooltip : IDisposable
    {
        public readonly ReactiveProperty<RectTransform> target = new ReactiveProperty<RectTransform>();

        public virtual void Dispose()
        {
            target.Dispose();
        }
    }
}
