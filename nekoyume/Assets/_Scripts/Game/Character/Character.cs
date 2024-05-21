using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX.Skill;
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

        public readonly Dictionary<int, Func<BuffCastingVFX, IEnumerator>> BuffCastCoroutine = new();

#region Event
        public Action<int> OnBuff;
        public Action<int> OnCustomEvent;
#endregion Event

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

        // TODO: 이 클래스에 존재해야할 느낌은 아니지만, 중복 구현을 피하기 위해 일단 여기에 둠
        /// <summary>
        /// Stage.CoCustomEvent를 통해 실행된 이벤트를 받아 처리하기 위해 생성
        /// </summary>
        /// <param name="customEventId">이벤트 ID</param>
        public void CustomEvent(int customEventId)
        {
            OnCustomEvent?.Invoke(customEventId);
        }
    }
}
