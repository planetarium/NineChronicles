using System;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class Tooltip : IDisposable
    {
        public readonly ReactiveProperty<RectTransform> target = new();

        public virtual void Dispose()
        {
            target.Dispose();
        }
    }
}
