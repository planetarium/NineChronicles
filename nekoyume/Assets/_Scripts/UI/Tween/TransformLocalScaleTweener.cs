using DG.Tweening;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    public class TransformLocalScaleTweener : BaseTweener
    {
        [SerializeField]
        private float delayForPlay = 0f;

        [SerializeField]
        private Ease easeForPlay = Ease.Linear;

        [SerializeField]
        private float delayForPlayReverse = 0f;

        [SerializeField]
        private Ease easeForPlayReverse = Ease.Linear;

        [SerializeField]
        private float3 end = float3.zero;

        [SerializeField]
        private float duration = 1f;

        [SerializeField]
        private bool isFrom = false;

        [SerializeField]
        private bool isLoop = true;

        [SerializeField]
        private LoopType loopType = LoopType.Incremental;

        private Transform _transformCache;
        private float3? _originLocalScaleCache;

        public Transform Transform => _transformCache
            ? _transformCache
            : _transformCache = transform;

        private float3 OriginLocalScale =>
            _originLocalScaleCache
            ?? (_originLocalScaleCache = Transform.localScale).Value;

        private void Awake()
        {
            Assert.NotNull(Transform);
            Assert.AreEqual(OriginLocalScale, _originLocalScaleCache);
        }

        public override Tweener PlayTween()
        {
            return PlayTween(duration);
        }

        public Tweener PlayTween(float newDuration)
        {
            ResetToOrigin();

            if (isFrom)
            {
                Transform.localScale = end;
                Tweener = DOTween
                    .To(() => Transform.localScale,
                        localScale => Transform.localScale = localScale,
                        OriginLocalScale,
                        newDuration)
                    .SetDelay(delayForPlay)
                    .SetEase(easeForPlay);
            }
            else
            {
                Transform.localScale = OriginLocalScale;
                Tweener = DOTween
                    .To(() => Transform.localScale,
                        localScale => Transform.localScale = localScale,
                        end,
                        newDuration)
                    .SetDelay(delayForPlay)
                    .SetEase(easeForPlay);
            }

            if (isLoop)
            {
                Tweener.SetLoops(-1, loopType);
            }

            return Tweener.Play();
        }

        public override Tweener PlayReverse()
        {
            return PlayReverse(duration);
        }

        public override void ResetToOrigin()
        {
            KillTween();
            Transform.localScale = OriginLocalScale;
        }

        public Tweener PlayReverse(float newDuration)
        {
            ResetToOrigin();

            if (isFrom)
            {
                Transform.localScale = OriginLocalScale;
                Tweener = DOTween
                    .To(() => Transform.localScale,
                        localScale => Transform.localScale = localScale,
                        end,
                        newDuration)
                    .SetDelay(delayForPlayReverse)
                    .SetEase(easeForPlayReverse);
            }
            else
            {
                Transform.localScale = end;
                Tweener = DOTween
                    .To(() => Transform.localScale,
                        localScale => Transform.localScale = localScale,
                        OriginLocalScale,
                        newDuration)
                    .SetDelay(delayForPlayReverse)
                    .SetEase(easeForPlayReverse);
            }

            if (isLoop)
            {
                Tweener.SetLoops(-1, loopType);
            }

            return Tweener.Play();
        }

        public void ResetToOriginalLocalScale()
        {
            Transform.localScale = OriginLocalScale;
        }
    }
}
