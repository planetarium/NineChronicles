using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class NpcFactory : MonoBehaviour
    {
        public GameObject Create(int id, Vector3 position)
        {
            var objectPool = GetComponent<ObjectPool>();
            if (ReferenceEquals(objectPool, null))
            {
                throw new NotFoundComponentException<ObjectPool>();
            }

            var npc = objectPool.Get<Character.Npc>(position);
            if (ReferenceEquals(npc, null))
            {
                throw new NotFoundComponentException<Character.Npc>();
            }
            var prevAnim = npc.GetComponentInChildren<Animator>(true);
            if (prevAnim)
            {
                Destroy(prevAnim.gameObject);
            }

            var origin = Resources.Load<GameObject>($"Character/Npc/{id}");
            var go = Instantiate(origin, npc.transform);
            npc.animator.ResetTarget(go);
            return npc.gameObject;
        }
    }
}
