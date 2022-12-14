using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class FriendCharacter : Character
    {
        private const float AnimatorTimeScale = 1.2f;
        private HudContainer _hudContainer;

        [SerializeField]
        private CharacterAppearance appearance;

        private void Awake()
        {
            Animator = new PlayerAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale;
            Animator.Idle();
        }

        public void Set(
            AvatarState avatarState,
            List<Costume> costumes,
            List<Equipment> equipments)
        {
            _hudContainer ??= Widget.Create<HudContainer>(true);
            _hudContainer.transform.localPosition = Vector3.left * 200000;
            appearance.Set(
                Animator,
                _hudContainer,
                costumes,
                equipments,
                avatarState.ear,
                avatarState.lens,
                avatarState.hair,
                avatarState.tail);
        }
    }
}
