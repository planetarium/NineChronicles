using UnityEngine;
using UnityEngine.Rendering;
using Enemy = Nekoyume.Model.Enemy;

namespace Nekoyume.Game.Factory
{
    public class EnemyFactory : MonoBehaviour
    {
        public GameObject Create(Enemy spawnCharacter, Vector2 position, Character.Player player)
        {
            var objectPool = Game.instance.stage.objectPool;
            var enemy = objectPool.Get<Character.Enemy>(position);
            if (!enemy)
                throw new NotFoundComponentException<Character.Enemy>();

            enemy.Set(spawnCharacter, player, true);

            // y좌표값에 따른 정렬 처리
            var sortingGroup = enemy.GetComponent<SortingGroup>();
            if (!sortingGroup)
                throw new NotFoundComponentException<SortingGroup>();

            sortingGroup.sortingOrder = (int) (position.y * 10) * -1;
            return enemy.gameObject;
        }
    }
}
