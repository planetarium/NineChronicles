using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Util;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class NPCFactory : MonoBehaviour
    {
        public GameObject Create(int id, Vector3 position, LayerType layerType, int sortingOrder)
        {
            var objectPool = GetComponent<ObjectPool>();
            if (ReferenceEquals(objectPool, null))
            {
                throw new NotFoundComponentException<ObjectPool>();
            }

            var npc = objectPool.Get<NPC>(position);
            if (ReferenceEquals(npc, null))
            {
                throw new NotFoundComponentException<NPC>();
            }

            var prevAnim = npc.GetComponentInChildren<Animator>(true);
            if (prevAnim)
            {
                Destroy(prevAnim.gameObject);
            }

            npc.SetSortingLayer(layerType, sortingOrder);
            var prefab = Resources.Load<GameObject>($"Character/NPC/{id}");
            var go = Instantiate(prefab, npc.transform);
            npc.ResetAnimatorTarget(go);
            return npc.gameObject;
        }

        public GameObject CreateDialogNPC(string key, Vector3 position, LayerType layerType, int sortingOrder)
        {
            var objectPool = GetComponent<ObjectPool>();
            if (ReferenceEquals(objectPool, null))
            {
                throw new NotFoundComponentException<ObjectPool>();
            }

            var npc = objectPool.Get<DialogNPC>(position);
            if (ReferenceEquals(npc, null))
            {
                throw new NotFoundComponentException<DialogNPC>();
            }

            var prevAnim = npc.GetComponentInChildren<Animator>(true);
            if (prevAnim)
            {
                Destroy(prevAnim.gameObject);
            }

            npc.SetSortingLayer(layerType, sortingOrder);
            var prefab = Resources.Load<GameObject>($"Character/NPC/{key}");
            var go = Instantiate(prefab, npc.transform);
            npc.ResetAnimatorTarget(go);
            return npc.gameObject;
        }
    }
}
