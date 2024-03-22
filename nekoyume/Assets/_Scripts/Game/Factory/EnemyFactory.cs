using Nekoyume.Game.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Game.VFX;
using UnityEngine;
using UnityEngine.Rendering;
using Enemy = Nekoyume.Model.Enemy;

namespace Nekoyume.Game.Factory
{
    public class EnemyFactory : MonoBehaviour
    {
        public static GameObject Create(Enemy spawnCharacter, Vector2 position, Character.Player player)
        {
            var objectPool = Stage.instance.objectPool;
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

        public static EnemyPlayer Create(Model.EnemyPlayer spawnCharacter, Vector2 position)
        {
            var objectPool = Stage.instance.objectPool;
            var enemy = objectPool.Get<Character.EnemyPlayer>(position);
            if (!enemy)
                throw new NotFoundComponentException<Character.EnemyPlayer>();

            var player = Stage.instance.GetPlayer();
            enemy.Set(spawnCharacter, player,true);

            // y좌표값에 따른 정렬 처리
            var sortingGroup = enemy.GetComponent<SortingGroup>();
            if (!sortingGroup)
                throw new NotFoundComponentException<SortingGroup>();

            sortingGroup.sortingOrder = (int) (position.y * 10) * -1;
            return enemy;
        }

        public static GameObject Create(int characterId, Vector2 position, float offset, Player target,
            bool summonEffect = false)
        {
            var objectPool = Stage.instance.objectPool;
            var enemy = objectPool.Get<PrologueCharacter>(new Vector2(position.x + offset, position.y));
            if (!enemy)
                throw new NotFoundComponentException<PrologueCharacter>();

            enemy.Set(characterId, target);
            if (summonEffect)
            {
                var effect = objectPool.Get<BattleSummonVFX>();
                var effectPosition = new Vector2(position.x, position.y + 0.55f);
                effect.gameObject.transform.position = effectPosition;
                effect.Play();
            }

            // y좌표값에 따른 정렬 처리
            // var sortingGroup = enemy.GetComponent<SortingGroup>();
            // if (!sortingGroup)
            //     throw new NotFoundComponentException<SortingGroup>();

            // sortingGroup.sortingOrder = (int) (position.y * 10) * -1;
            return enemy.gameObject;
        }

    }
}
