using UnityEngine;

namespace Nekoyume.Game.VFX.Skill
{
    public class SkillDoubleVFX : SkillVFX
    {
        public GameObject first;
        public ParticleSystem second;

        public void FirstStrike()
        {
            var ps = GetComponent<ParticleSystem>();
            var emission = ps.emission;
            emission.enabled = true;
            foreach (Transform child in first.transform)
            {
                child.gameObject.SetActive(true);
            }
            second.gameObject.SetActive(false);
            Play();
        }

        public void SecondStrike()
        {
            var ps = GetComponent<ParticleSystem>();
            var emission = ps.emission;
            emission.enabled = false;
            foreach (Transform child in first.transform)
            {
                child.gameObject.SetActive(false);
            }
            second.gameObject.SetActive(true);
            Play();
        }
    }
}
