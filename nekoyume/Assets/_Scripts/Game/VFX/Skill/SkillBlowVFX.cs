using UnityEngine;

namespace Nekoyume.Game.VFX.Skill
{
    public class SkillBlowVFX : SkillVFX
    {
        public ParticleSystem ground;

        public override void Play()
        {
            if (ground)
            {
                ground.transform.position = target is null ? go.transform.position : target.transform.position;
            }
            base.Play();
        }
    }
}
