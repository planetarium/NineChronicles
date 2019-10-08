using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Factory;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Game.Trigger
{
    public class MonsterSpawner : MonoBehaviour
    {
        public Vector3[] spawnPoints;

        private Enemy _enemy;

        private int _stageId;
        private int _wave;
        private const float SpawnOffset = 6.0f;

        public void SetData(int stageId, Enemy enemy)
        {
            _stageId = stageId;
            _enemy = enemy;
            SpawnWave();
        }

        private void SpawnWave()
        {
            var factory = Game.instance.stage.enemyFactory;
            var player = Game.instance.stage.GetComponentInChildren<Character.Player>();
            var offsetX = player.transform.position.x + 2.8f;
            var randIndex = Enumerable.Range(0, spawnPoints.Length / 2)
                .OrderBy(n => Guid.NewGuid()).ToArray();
            {
                var r = randIndex[0];
                var pos = new Vector2(
                    spawnPoints[r].x + offsetX,
                    spawnPoints[r].y);
                factory.Create(_enemy, pos, player);
            }
        }

        public IEnumerator CoSetData(int stageId, List<Enemy> monsters)
        {
            _stageId = stageId;
            yield return StartCoroutine(CoSpawnWave(monsters));
        }

        private IEnumerator CoSpawnWave(List<Enemy> monsters)
        {
            var stage = Game.instance.stage;
            for (var index = 0; index < monsters.Count; index++)
            {
                var monster = monsters[index];
                monster.spawnIndex = index;

                var player = stage.GetComponentInChildren<Character.Player>();
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

        private IEnumerator CoSpawnMonster(Enemy enemy, Vector2 pos, Character.Player player)
        {
            Game.instance.stage.enemyFactory.Create(enemy, pos, player);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.0f, 0.2f));
        }

        public class InvalidWaveException: Exception
        {}
    }
}
