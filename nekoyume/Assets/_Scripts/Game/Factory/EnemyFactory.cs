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
        public GameObject Create(Monster spawnCharacter, Vector2 position, Character.Player player)
        {
            var objectPool = GetComponent<ObjectPool>();
            if (ReferenceEquals(objectPool, null))
            {
                throw new NotFoundComponentException<ObjectPool>();
            }

            var enemy = objectPool.Get<Enemy>(position);
            if (ReferenceEquals(enemy, null))
            {
                throw new NotFoundComponentException<Enemy>();
            }
            var prevAnim = enemy.GetComponentInChildren<Animator>(true);
            if (prevAnim)
            {
                Destroy(prevAnim.gameObject);
            }
            //FIXME 애니메이터 재사용시 기존 투명도가 유지되는 문제가 있음.
//            var animator = objectPool.Get(spawnCharacter.data.Id.ToString(), true);
            var origin = Resources.Load<GameObject>($"Character/Monster/{spawnCharacter.data.Id}") ??
                         Resources.Load<GameObject>($"Character/Monster/{DefaultResource}");
            var go = Instantiate(origin, enemy.transform);
            enemy.animator.ResetTarget(go);
            enemy.Init(spawnCharacter, player);

            // y좌표값에 따른 정렬 처리
            var sortingGroup = enemy.GetComponent<SortingGroup>();
            if (ReferenceEquals(sortingGroup, null))
            {
                throw new NotFoundComponentException<SortingGroup>();
            }
            sortingGroup.sortingOrder = (int) (position.y * 10) * -1;
            return enemy.gameObject;
        }
    }
}
