using System;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Character;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public abstract class Character : MonoBehaviour
    {
        public Guid Id {get; protected set; }
        public SizeType SizeType { get; protected set; }
        public CharacterAnimator Animator { get; protected set; }
        protected virtual Vector3 HUDOffset => Animator.GetHUDPosition();
        protected Vector3 HealOffset => Animator.HealPosition;
        protected bool AttackEndCalled { get; set; }

        protected System.Action ActionPoint;

        protected void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "AttackStart":
                case "attackStart":
                    AudioController.PlaySwing();
                    break;
                case "AttackPoint":
                case "attackPoint":
                    AttackEndCalled = true;
                    ActionPoint?.Invoke();
                    ActionPoint = null;
                    break;
                case "Footstep":
                case "footstep":
                    AudioController.PlayFootStep();
                    break;
            }
        }
    }
}
