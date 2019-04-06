using Nekoyume.Data;
using Nekoyume.Game.Character;
using Nekoyume.Game.Character.Boss;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Game.Factory
{
    public class EnemyFactory : MonoBehaviour
    {
        public GameObject Create(Monster spawnCharacter, Vector2 position)
        {
            var objectPool = GetComponent<ObjectPool>();
            if (ReferenceEquals(objectPool, null))
            {
                throw new NotFoundComponentException<ObjectPool>();
            }

            var go = objectPool.Get("Enemy", true, position);
            //FIXME 애니메이터 재사용시 기존 투명도가 유지되는 문제가 있음.
//            var animator = objectPool.Get(spawnCharacter.data.Id.ToString(), true);
            var origin = Resources.Load<GameObject>($"Prefab/{spawnCharacter.data.id}");
            var animator = Instantiate(origin, go.transform);
            var enemy = animator.GetComponent<Enemy>();
            if (ReferenceEquals(enemy, null))
            {
                throw new NotFoundComponentException<Enemy>();
            }
            enemy.Init(spawnCharacter);
            return go;
        }
    }
}
