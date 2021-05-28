using System;
using Nekoyume.EnumType;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nekoyume.Game.Character
{
    using UniRx;

    public class DialogNPC : MonoBehaviour
    {
        private const float AnimatorTimeScale = 1.2f;

        private SortingGroup _sortingGroup;

        private NPCAnimator Animator { get; set; }
        public NPCSpineController SpineController { get; private set; }

        private void Awake()
        {
            _sortingGroup = GetComponent<SortingGroup>();

            Animator = new NPCAnimator(this) { TimeScale = AnimatorTimeScale };
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
        }

        private void OnAnimatorEvent(string eventName)
        {
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
        }

        public void Idle()
        {
            // Animator.Idle();
        }
    }
}
