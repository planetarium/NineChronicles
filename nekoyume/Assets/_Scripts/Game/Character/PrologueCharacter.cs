using System;
using System.Linq;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class PrologueCharacter : MonoBehaviour
    {
        public CharacterAnimator Animator { get; protected set; }
        public string TargetTag { get; protected set; }
        private CharacterSpineController SpineController { get; set; }
        private void Awake()
        {
            Animator = new EnemyAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = 1.2f;

            TargetTag = Tag.Player;
        }

        public void Set(int characterId)
        {
            var spineResourcePath = $"Character/Monster/{characterId}";

            if (!(Animator.Target is null))
            {
                var animatorTargetName = spineResourcePath.Split('/').Last();
                if (Animator.Target.name.Contains(animatorTargetName))
                    return;

                Animator.DestroyTarget();
            }

            var origin = Resources.Load<GameObject>(spineResourcePath);
            var go = Instantiate(origin, gameObject.transform);
            SpineController = go.GetComponent<CharacterSpineController>();
            Animator.ResetTarget(go);
            Animator.Standing();
        }

        private void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "attackStart":
                    AudioController.PlaySwing();
                    break;
                case "attackPoint":
                    // AttackEndCalled = true;
                    break;
                case "footstep":
                    AudioController.PlayFootStep();
                    break;
            }
        }
    }
}
