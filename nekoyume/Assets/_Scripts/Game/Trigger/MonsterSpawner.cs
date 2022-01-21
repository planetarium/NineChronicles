using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nekoyume.Game.Trigger
{
    public class MonsterSpawner : MonoBehaviour
    {
        public Vector3[] spawnPoints;

        private Model.Enemy _enemy;

        private int _wave;
        private const float SpawnOffset = 6.0f;

        public void SetData(Model.Enemy enemy)
        {
            _enemy = enemy;
            SpawnWave();
        }

        private void SpawnWave()
        {
            var player = Game.instance.Stage.SelectedPlayer;
            var offsetX = player.transform.position.x + 2.8f;
            var randIndex = Enumerable.Range(0, spawnPoints.Length / 2)
                .OrderBy(n => Guid.NewGuid()).ToArray();
            {
                var r = randIndex[0];
                var pos = new Vector2(
                    spawnPoints[r].x + offsetX,
                    spawnPoints[r].y);
                EnemyFactory.Create(_enemy, pos, player);
            }
        }

        public IEnumerator CoSetData(List<Model.Enemy> monsters)
        {
            yield return StartCoroutine(CoSpawnWave(monsters));
        }

        private IEnumerator CoSpawnWave(List<Model.Enemy> monsters)
        {
            var stage = Game.instance.Stage;
            for (var index = 0; index < monsters.Count; index++)
            {
                var monster = monsters[index];
                monster.spawnIndex = index;

                var player = stage.SelectedPlayer;
                var offsetX = player.transform.position.x + SpawnOffset;
                {
                    Vector3 point;
                    try
                    {
                        point = spawnPoints[index];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidWaveException();
                    }
                    var pos = new Vector2(
                        point.x + offsetX,
                        point.y);
                    yield return StartCoroutine(CoSpawnMonster(monster, pos, player));
                }
            }
        }

        private static IEnumerator CoSpawnMonster(Model.Enemy enemy, Vector2 pos, Character.Player player)
        {
            EnemyFactory.Create(enemy, pos, player);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.0f, 0.2f));
        }

        public class InvalidWaveException: Exception
        {}

        public IEnumerator CoSetData(Model.EnemyPlayer enemyPlayer, Vector3 offset)
        {
            yield return StartCoroutine(CoSpawnEnemy(enemyPlayer, offset));
        }

        private static IEnumerator CoSpawnEnemy(Model.EnemyPlayer enemy, Vector2 pos)
        {
            var enemyPlayer = EnemyFactory.Create(enemy, pos);
            enemyPlayer.StartRun();
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.0f, 0.2f));
        }

        public IEnumerator CoSpawnWave(List<int> monsterIds, Vector2 position, float offset, PrologueCharacter fenrir, Player player)
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.FenrirGrowlSummon);
            for (var index = 0; index < monsterIds.Count; index++)
            {
                fenrir.Animator.Cast();
                var id = monsterIds[index];
                var pos = new Vector2(
                    spawnPoints[index].x + position.x + offset,
                    spawnPoints[index].y);
                var go = EnemyFactory.Create(id, pos, offset, player, true);
                var enemy = go.GetComponent<PrologueCharacter>();
                yield return new WaitUntil(() => enemy.Animator.IsIdle());
                yield return new WaitForSeconds(1f);
                fenrir.Animator.Idle();
                yield return new WaitForSeconds(0.3f);
            }
        }
    }
}
