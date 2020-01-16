using System;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class NpcAnimator
    {
        private readonly Npc _root;
        private readonly float _timeScale;
        private Animator _animator;

        private int _baseLayerIndex;

        public NpcAnimator(Npc root, float animatorTimeScale)
        {
            _root = root;
            _animator = root.GetComponentInChildren<Animator>();
            _timeScale = animatorTimeScale;
        }

        public void Idle()
        {
            _animator.Play(nameof(CharacterAnimation.Type.Idle), _baseLayerIndex, 0f);
        }

        public void Touch()
        {
            _animator.Play(nameof(CharacterAnimation.Type.Touch), _baseLayerIndex, 0f);
            _animator.SetBool(nameof(CharacterAnimation.Type.Touch), true);
        }

        public void Emotion()
        {
            _animator.Play(nameof(CharacterAnimation.Type.Emotion), _baseLayerIndex, 0f);
        }

        public void Greeting()
        {
            _animator.Play(nameof(CharacterAnimation.Type.Greeting), _baseLayerIndex, 0f);
        }

        public void Appear()
        {
            _animator.Play(nameof(CharacterAnimation.Type.Appear), _baseLayerIndex, 0f);
        }

        public void ResetTarget(GameObject value)
        {
            if (!value)
            {
                throw new ArgumentNullException();
            }

            _animator = value.GetComponentInChildren<Animator>();

            if (_animator is null)
            {
                throw new NotFoundComponentException<Animator>();
            }

            _animator.speed = _timeScale;
            _baseLayerIndex = _animator.GetLayerIndex("Base Layer");
        }

    }
}
