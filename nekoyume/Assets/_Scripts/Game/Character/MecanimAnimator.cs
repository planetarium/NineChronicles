using System;
using Spine.Unity;
using UniRx;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nekoyume.Game.Character
{
    public class MecanimAnimator
    {
        protected int BaseLayerIndex { get; private set; }

        public GameObject Root { get; }
        public GameObject Target { get; private set; }
        public Subject<string> OnEvent { get; }
        public float TimeScale { get; set; }

        protected Animator Animator { get; private set; }

        public MecanimAnimator(GameObject root)
        {
            Root = root;
            OnEvent = new Subject<string>();
        }

        public void Dispose()
        {
            OnEvent?.Dispose();
        }

        public virtual void InitTarget(
            GameObject target,
            MeshRenderer meshRenderer,
            SkeletonAnimation skeletonAnimation,
            Animator animator)
        {
            Target = target;
            Animator = animator;
            Animator.speed = TimeScale;
            BaseLayerIndex = Animator.GetLayerIndex("Base Layer");
        }

        public virtual void ResetTarget(GameObject value)
        {
            if (!value)
                throw new ArgumentNullException();

            Target = value;
            Animator = value.GetComponentInChildren<Animator>();
            if (Animator is null)
                throw new NotFoundComponentException<Animator>();

            Animator.speed = TimeScale;

            BaseLayerIndex = Animator.GetLayerIndex("Base Layer");
        }

        public void DestroyTarget()
        {
            if (Target is null)
            {
                NcDebug.LogWarning($"Target is null. {nameof(MecanimAnimator)}.{nameof(DestroyTarget)}");
                return;
            }

            Object.Destroy(Target);
            Target = null;
        }

        public virtual bool ValidateAnimator()
        {
            return Animator != null;
        }
    }
}
