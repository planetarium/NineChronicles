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

        public NPCAnimator Animator { get; private set; }
        public NPCSpineController SpineController { get; private set; }

        private void Awake()
        {
            _sortingGroup = GetComponent<SortingGroup>();
            _touchHandler = GetComponentInChildren<TouchHandler>();
            _touchHandler.OnClick.Subscribe(_ => PlayAnimation(NPCAnimation.Type.Touch_01)).AddTo(gameObject);
            
            Animator = new NPCAnimator(this) {TimeScale = AnimatorTimeScale};
        }

        private void OnEnable()
        {
            _sortingGroup.sortingLayerName = LayerType.InGameBackground.ToLayerName();
        }

        public void SetSortingLayer(LayerType layerType)
        {
            _sortingGroup.sortingLayerName = layerType.ToLayerName();
        }

        public void ResetTarget(GameObject target)
        {
            Animator.ResetTarget(target);
            SpineController = target.GetComponent<NPCSpineController>();
        }
        
        public void PlayAnimation(NPCAnimation.Type type)
        {
            Animator.Play(type);
        }
    }
}
