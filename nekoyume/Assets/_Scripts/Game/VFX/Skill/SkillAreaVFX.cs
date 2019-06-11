using UnityEngine;

namespace Nekoyume.Game.VFX.Skill
{
    public class SkillAreaVFX : SkillVFX
    {
        public ParticleSystem finisher;
        public ParticleSystem last;

        public override void Play()
        {
            if (finisher)
                finisher.gameObject.SetActive(false);
            base.Play();
        }

        public void Finisher()
        {
            if (finisher)
            {
                finisher.gameObject.SetActive(true);
                finisher.Play();
            }
        }
    }
}
