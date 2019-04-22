using Nekoyume.Game.Character;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nekoyume.Game.Factory
{
    public class EnemyFactory : MonoBehaviour
    {
        private const int DefaultResource = 201000;
        public GameObject Create(Monster spawnCharacter, Vector2 position)
        {
            var objectPool = GetComponent<ObjectPool>();
            if (ReferenceEquals(objectPool, null))
            {
                throw new NotFoundComponentException<ObjectPool>();
            }

            var go = objectPool.Get("Enemy", true, position);
            var prevAnim = go.GetComponentInChildren<Animator>(true);
            if (prevAnim)
            {
                Destroy(prevAnim.gameObject);
            }
            //FIXME 애니메이터 재사용시 기존 투명도가 유지되는 문제가 있음.
//            var animator = objectPool.Get(spawnCharacter.data.Id.ToString(), true);
            var origin = Resources.Load<GameObject>($"Prefab/{spawnCharacter.data.characterResource}") ??
                         Resources.Load<GameObject>($"Prefab/{DefaultResource}");
            var animator = Instantiate(origin, go.transform);
            var enemy = animator.GetComponent<Enemy>();
            if (ReferenceEquals(enemy, null))
            {
                throw new NotFoundComponentException<Enemy>();
            }
            enemy.Init(spawnCharacter);

            // y좌표값에 따른 정렬 처리
            var sortingGroup = go.GetComponent<SortingGroup>();
            if (ReferenceEquals(sortingGroup, null))
            {
                throw new NotFoundComponentException<SortingGroup>();
            }
            sortingGroup.sortingOrder = (int) (position.y * 10) * -1;
            return go;
        }
    }
}
