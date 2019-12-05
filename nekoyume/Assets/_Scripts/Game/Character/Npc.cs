using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Npc : MonoBehaviour
    {
        private const float AnimatorTimeScale = 1.2f;

        public NpcAnimator animator;
        public TouchHandler touchHandler;

        private void Awake()
        {
            animator = new NpcAnimator(this, AnimatorTimeScale);
            touchHandler.OnClick.Subscribe(_ =>
            {
                Touch();
            }).AddTo(gameObject);
        }

        public void Greeting()
        {
            animator.Greeting();
        }

        public void Idle()
        {
            animator.Idle();
        }

        public void Emotion()
        {
            animator.Emotion();
        }

        private void Touch()
        {
            animator.Touch();
        }

        public void Appear()
        {
            animator.Appear();
        }
    }
}
