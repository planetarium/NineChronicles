using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class NPC : MonoBehaviour
    {
        private const float AnimatorTimeScale = 1.2f;

        public NPCAnimator Animator { get; private set; }
        public TouchHandler touchHandler;

        private void Awake()
        {
            Animator = new NPCAnimator(this) {TimeScale = AnimatorTimeScale};
            touchHandler.OnClick.Subscribe(_ => PlayAnimation(NPCAnimation.Type.Touch_01)).AddTo(gameObject);
        }
        
        public void PlayAnimation(NPCAnimation.Type type)
        {
            Animator.Play(type);
        }
    }
}
