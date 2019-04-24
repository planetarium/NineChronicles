using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Nekoyume.Game.Factory;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Game.Trigger
{
    public class MonsterSpawner : MonoBehaviour
    {
        public Vector3[] spawnPoints;

        private Monster _monster;

        private Stage _stage;
        private int _stageId;
        private int _wave;
        private EnemyFactory _factory;
        private const float SpawnOffset = 6.0f;

        private void Start()
        {
            _factory = GetComponentInParent<EnemyFactory>();
            _stage = GetComponentInParent<Stage>();
        }

        public void SetData(int stageId, Monster monster)
        {
            _stageId = stageId;
            _monster = monster;
            SpawnWave();
        }

        private void SpawnWave()
        {
            var factory = GetComponentInParent<EnemyFactory>();
            var player = _stage.GetComponentInChildren<Character.Player>();
            var offsetX = player.transform.position.x + 2.8f;
            var randIndex = Enumerable.Range(0, spawnPoints.Length / 2)
                .OrderBy(n => Guid.NewGuid()).ToArray();
            {
                var r = randIndex[0];
                var pos = new Vector2(
                    spawnPoints[r].x + offsetX,
                    spawnPoints[r].y);
                factory.Create(_monster, pos, player);
            }
        }

        public void SetData(int stageId, List<Monster> monsters)
        {
            _stageId = stageId;
            StartCoroutine(CoSpawnWave(monsters));
        }

        private IEnumerator CoSpawnWave(List<Monster> monsters)
        {
            for (var index = 0; index < monsters.Count; index++)
            {
                var monster = monsters[index];
                var player = _stage.GetComponentInChildren<Character.Player>();
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

        private IEnumerator CoSpawnMonster(Monster monster, Vector2 pos, Character.Player player)
        {
            _factory.Create(monster, pos, player);
            yield return new WaitForSeconds(0.1f);
        }

        public class InvalidWaveException: Exception
        {}
    }
}
