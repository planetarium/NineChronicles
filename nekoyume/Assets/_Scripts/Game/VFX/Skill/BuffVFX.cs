using UnityEngine;
using Nekoyume.Game.Character;

namespace Nekoyume.Game.VFX.Skill
{
    public class BuffVFX : VFX
    {
        [field: SerializeField]
        public Actor Target { get; set; }

        [field: SerializeField]
        public virtual bool IsPersisting { get; set; } = false;

#if UNITY_EDITOR
        [field: SerializeField]
        public ParticleSystemRenderer[] BackgroundParticleSystems;
#endif

        public override void Play()
        {
            base.Play();

            if (IsPersisting)
            {
                _isPlaying = true;
            }
        }
    }
}
