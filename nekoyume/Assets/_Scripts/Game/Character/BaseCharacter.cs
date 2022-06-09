using System;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Character;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public abstract class BaseCharacter : MonoBehaviour
    {
        public Guid Id {get; protected set; }
        public SizeType SizeType { get; protected set; }
        public CharacterAnimator Animator { get; protected set; }
        protected Vector3 HUDOffset => Animator.GetHUDPosition();
        protected Vector3 HealOffset => Animator.HealPosition;
        protected bool AttackEndCalled { get; set; }

        protected void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "attackStart":
                    AudioController.PlaySwing();
                    break;
                case "attackPoint":
                    AttackEndCalled = true;
                    break;
                case "footstep":
                    AudioController.PlayFootStep();
                    break;
            }
        }
    }
}
