using Nekoyume.Model;
using UnityEngine;
using UnityEngine.Rendering;
using Enemy = Nekoyume.Model.Enemy;

namespace Nekoyume.Game.Factory
{
    public class EnemyFactory : MonoBehaviour
    {
        public static GameObject Create(Enemy spawnCharacter, Vector2 position, Character.Player player)
        {
            var objectPool = Game.instance.Stage.objectPool;
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

        public static GameObject Create(EnemyPlayer spawnCharacter, Vector2 position)
        {
            var objectPool = Game.instance.Stage.objectPool;
            var enemy = objectPool.Get<Character.EnemyPlayer>(position);
            if (!enemy)
                throw new NotFoundComponentException<Character.EnemyPlayer>();

            var player = Game.instance.Stage.GetPlayer();
            enemy.Set(spawnCharacter, player,true);
            enemy.StartRun();

            // y좌표값에 따른 정렬 처리
            var sortingGroup = enemy.GetComponent<SortingGroup>();
            if (!sortingGroup)
                throw new NotFoundComponentException<SortingGroup>();

            sortingGroup.sortingOrder = (int) (position.y * 10) * -1;
            return enemy.gameObject;
        }

    }
}
