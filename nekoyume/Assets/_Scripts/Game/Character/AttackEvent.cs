using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class AttackEvent : MonoBehaviour
    {
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

        public void Dummy()
        {
        }
    }
}
