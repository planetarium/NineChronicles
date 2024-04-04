using Nekoyume.Game.Character;
using Nekoyume.Game.VFX;
using UnityEngine;
using UnityEngine.Rendering;
using Enemy = Nekoyume.Model.Enemy;

namespace Nekoyume.Game.Factory
{
    public class StageMonsterFactory : MonoBehaviour
    {
        public static GameObject Create(Enemy spawnCharacter, Vector2 position, Player player)
        {
            var objectPool = Game.instance.Stage.objectPool;
            var enemy = objectPool.Get<StageMonster>(position);
            if (!enemy)
                throw new NotFoundComponentException<StageMonster>();

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
            var objectPool = Game.instance.Stage.objectPool;
            var enemy = objectPool.Get<EnemyPlayer>(position);
            if (!enemy)
                throw new NotFoundComponentException<EnemyPlayer>();

            var player = Game.instance.Stage.GetPlayer();
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
            var objectPool = Game.instance.Stage.objectPool;
            var enemy = objectPool.Get<PrologueCharacter>(new Vector2(position.x + offset, position.y));
            if (!enemy)
                throw new NotFoundComponentException<PrologueCharacter>();

            enemy.Set(characterId, target);

            if (!summonEffect)
            {
                return enemy.gameObject;
            }

            var effect         = objectPool.Get<BattleSummonVFX>();
            var effectPosition = new Vector2(position.x, position.y + 0.55f);
            effect.gameObject.transform.position = effectPosition;
            effect.Play();

            return enemy.gameObject;
        }
    }
}
