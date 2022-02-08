using System;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class RefreshButton : MonoBehaviour
    {
        private const float AnimatorTimeScale = 1.2f;

        private SortingGroup _sortingGroup;

        private NPCAnimator Animator { get; set; }
        public TouchHandler TouchHandler { get; private set; }
        public NPCSpineController SpineController { get; private set; }

        private void Awake()
        {
            _sortingGroup = GetComponent<SortingGroup>();
            TouchHandler = GetComponentInChildren<TouchHandler>();

            TouchHandler.OnClick
                .Merge(TouchHandler.OnDoubleClick)
                .Merge(TouchHandler.OnMultipleClick)
                .Subscribe(_ => PlayAnimation(NPCAnimation.Type.Click))
                .AddTo(gameObject);
            TouchHandler.OnLeftDown
                .Subscribe(_ => PlayAnimation(NPCAnimation.Type.Over))
                .AddTo(gameObject);

            Animator = new NPCAnimator(this) {TimeScale = AnimatorTimeScale};
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
        }
        
        private void Start()
        {
            UpdateAnimatorTarget();
        }

        public void SetSortingLayer(LayerType layerType)
        {
            SetSortingLayer(layerType, _sortingGroup.sortingOrder);
        }

        public void SetSortingLayer(LayerType layerType, int sortingOrder)
        {
            _sortingGroup.sortingLayerName = layerType.ToLayerName();
            _sortingGroup.sortingOrder = sortingOrder;
        }

        public void ResetAnimatorTarget(GameObject target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            Animator.ResetTarget(target);
            SpineController = target.GetComponentInChildren<NPCSpineController>();
            if (SpineController is null)
            {
                throw new NotFoundComponentException<NPCSpineController>();
            }

            TouchHandler.SetCollider(
                SpineController.BoxCollider,
                target.transform.localPosition,
                target.transform.localScale);
        }

        public void PlayAnimation(NPCAnimation.Type type)
        {
            Animator.Play(type);
        }

        private void UpdateAnimatorTarget()
        {
            var target = GetComponentInChildren<NPCSpineController>();
            if (target is null)
            {
                return;
            }

            ResetAnimatorTarget(target.gameObject);
        }

        protected void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "Smash":
                {
                    AudioController.instance.PlaySfx(AudioController.SfxCode.CombinationSmash);
                    var position = ActionCamera.instance.Cam.transform.position;
                    VFXController.instance.CreateAndChaseCam<HammerSmashVFX>(
                        position,
                        new Vector3(0.7f, -0.25f));
                    break;
                }
                case "emotion":
                {
                    var bodyBone = SpineController.SkeletonAnimation.skeleton.FindBone("body_01");
                    var spineControllerTransform = SpineController.transform;
                    var position = bodyBone?.GetWorldPosition(spineControllerTransform)
                                   ?? spineControllerTransform.position;
                    VFXController.instance.CreateAndChaseCam<EmotionHeartVFX>(
                        position,
                        new Vector3(0f, 0f, -10f));
                    break;
                }
            }
        }
    }
}
