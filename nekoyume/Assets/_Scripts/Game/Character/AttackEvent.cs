using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class AttackEvent : MonoBehaviour
    {
        public void AttackEnd()
        {
            Event.OnAttackEnd.Invoke();
        }
    }
}
