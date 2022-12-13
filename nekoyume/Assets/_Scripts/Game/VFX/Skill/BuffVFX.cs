using UnityEngine;
using Nekoyume.Game.Character;

namespace Nekoyume.Game.VFX.Skill
{
    public class BuffVFX : VFX
    {
        [field: SerializeField]
        public CharacterBase Target { get; set; }

        [field: SerializeField]
        public bool IsPersisting { get; set; } = false;

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
