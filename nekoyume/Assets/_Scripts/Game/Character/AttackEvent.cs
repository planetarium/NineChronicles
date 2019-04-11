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
            Event.OnAttackEnd.Invoke();
        }

        public void HitEnd()
        {
            Event.OnHitEnd.Invoke();
        }

        public void DieEnd()
        {
            Event.OnDieEnd.Invoke();
        }
    }
}
