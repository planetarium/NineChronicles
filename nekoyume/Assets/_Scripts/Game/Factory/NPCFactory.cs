using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class NPCFactory : MonoBehaviour
    {
        public GameObject Create(int id, Vector3 position)
        {
            var objectPool = GetComponent<ObjectPool>();
            if (ReferenceEquals(objectPool, null))
            {
                throw new NotFoundComponentException<ObjectPool>();
            }

            var npc = objectPool.Get<Character.NPC>(position);
            if (ReferenceEquals(npc, null))
            {
                throw new NotFoundComponentException<Character.NPC>();
            }
            var prevAnim = npc.GetComponentInChildren<Animator>(true);
            if (prevAnim)
            {
                Destroy(prevAnim.gameObject);
            }

            var origin = Resources.Load<GameObject>($"Character/NPC/{id}");
            var go = Instantiate(origin, npc.transform);
            npc.ResetTarget(go);
            return npc.gameObject;
        }
    }
}
