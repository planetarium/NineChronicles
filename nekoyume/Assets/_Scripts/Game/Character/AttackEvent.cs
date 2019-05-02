using Nekoyume.Game.Controller;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class AttackEvent : MonoBehaviour
    {
        public void PlayAttackSfx()
        {
            AudioController.PlaySwing();
        }

        public void PlayFootStepSfx()
        {
            AudioController.PlayFootStep();
        }
        
        public void AttackEnd()
        {
            var character = GetComponentInParent<CharacterBase>();
            Event.OnAttackEnd.Invoke(character);
        }
    }
}
