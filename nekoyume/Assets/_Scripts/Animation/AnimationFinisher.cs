using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [RequireComponent(typeof(Animator))]
    public class AnimationFinisher : MonoBehaviour
    {
        [SerializeField]
        private int targetLayer = 0;

        [SerializeField]
        private bool finishOnDisabled = true;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.keepAnimatorStateOnDisable = true;
        }

        private void OnDisable()
        {
            if (finishOnDisabled)
            {
                FinishAnimation();
            }
        }

        public void FinishAnimation()
        {
            var hash = _animator.GetCurrentAnimatorStateInfo(targetLayer).fullPathHash;
            _animator.Play(hash, targetLayer, 1);
        }
    }
}
