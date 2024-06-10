using Nekoyume.Game.Character;
using UnityEngine;

namespace Nekoyume.Game.VFX.Skill
{
    public class SkillVFX : VFX
    {
        public Character.Character target;
        public GameObject go;

#if UNITY_EDITOR
        [field: SerializeField]
        public ParticleSystemRenderer[] BackgroundParticleSystems;
#endif
    }
}
