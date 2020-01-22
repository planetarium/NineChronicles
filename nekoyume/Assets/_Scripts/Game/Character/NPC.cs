using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nekoyume.Game.Character
{
    [RequireComponent(typeof(SortingGroup))]
    public class NPC : MonoBehaviour
    {
        private const float AnimatorTimeScale = 1.2f;

        private SortingGroup _sortingGroup;
        private TouchHandler _touchHandler;
        [SerializeField] private bool _resetTargetOnStart = false;

        public NPCAnimator Animator { get; private set; }
        public NPCSpineController SpineController { get; private set; }

        private void Awake()
        {
            _sortingGroup = GetComponent<SortingGroup>();
            _touchHandler = GetComponentInChildren<TouchHandler>();
            _touchHandler.OnClick.Subscribe(_ => PlayAnimation(NPCAnimation.Type.Touch_01)).AddTo(gameObject);

            Animator = new NPCAnimator(this) {TimeScale = AnimatorTimeScale};
        }

        private void Start()
        {
            if (_resetTargetOnStart)
            {
                ResetTarget();
            }
        }

        private void OnEnable()
        {
            _sortingGroup.sortingLayerName = LayerType.InGameBackground.ToLayerName();
        }

        public void SetSortingLayer(LayerType layerType)
        {
            _sortingGroup.sortingLayerName = layerType.ToLayerName();
        }

        public void ResetTarget()
        {
            var target = GetComponentInChildren<NPCSpineController>();
            if (target is null)
                throw new NotFoundComponentException<NPCSpineController>();

            Animator.ResetTarget(target.gameObject);
            SpineController = target;
            _touchHandler.SetCollider(SpineController.BoxCollider, target.transform.localPosition,
                target.transform.localScale);
        }

        public void ResetTarget(GameObject target)
        {
            Animator.ResetTarget(target);
            SpineController = target.GetComponentInChildren<NPCSpineController>();
            _touchHandler.SetCollider(SpineController.BoxCollider, target.transform.localPosition,
                target.transform.localScale);
        }

        public void PlayAnimation(NPCAnimation.Type type)
        {
            Animator.Play(type);
        }
    }
}
