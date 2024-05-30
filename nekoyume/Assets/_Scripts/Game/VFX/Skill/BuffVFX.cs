using UnityEngine;
using Nekoyume.Game.Character;
using Nekoyume.Model.BattleStatus;

namespace Nekoyume.Game.VFX.Skill
{
    public class BuffVFX : VFX
    {
        [field: SerializeField]
        public Character.Character Target { get; set; }

        [field: SerializeField]
        public virtual bool IsPersisting { get; set; } = false;

        [field: SerializeField]
        public Model.Buff.Buff Buff { get; set; }

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
