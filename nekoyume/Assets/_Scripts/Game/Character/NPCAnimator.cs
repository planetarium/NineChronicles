using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class NPCAnimator : SkeletonAnimator
    {
        public NPCAnimator(NPC npc) : base(npc.gameObject)
        {
        }

        public void Play(NPCAnimation.Type type, float normalizedTime = 0f)
        {
            if (!ValidateAnimator())
                return;
            
            Debug.LogWarning($"{nameof(NPCAnimator)}.{nameof(Play)}: {type.ToString()}, {BaseLayerIndex}, {normalizedTime}");
            
            Animator.Play(type.ToString(), BaseLayerIndex, normalizedTime);
        }
    }
}
