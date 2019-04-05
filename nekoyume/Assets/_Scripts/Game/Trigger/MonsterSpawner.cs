using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Game.Trigger
{
    public class MonsterSpawner : MonoBehaviour
    {
        [SerializeField] private Vector3[] _spawnPoints;

        private Monster _monster;

        private Stage _stage;
        private int _stageId;
        private int _wave;
        private const float SpawnOffset = 2.8f;

        private void Start()
        {
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
            var randIndex = Enumerable.Range(0, _spawnPoints.Length / 2)
                .OrderBy(n => Guid.NewGuid()).ToArray();
            {
                var r = randIndex[0];
                var pos = new Vector2(
                    _spawnPoints[r].x + offsetX,
                    _spawnPoints[r].y);
                factory.Create(_monster, pos);
            }
        }

        public void SetData(int stageId, List<Monster> monsters)
        {
            _stageId = stageId;
            SpawnWave(monsters);
        }

        private void SpawnWave(List<Monster> monsters)
        {
            for (var index = 0; index < monsters.Count; index++)
            {
                var monster = monsters[index];
                var factory = GetComponentInParent<EnemyFactory>();
                var player = _stage.GetComponentInChildren<Character.Player>();
                var offsetX = player.transform.position.x + SpawnOffset;
                {
                    Vector3 point;
                    try
                    {
                        point = _spawnPoints[index];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidWaveException();
                    }
                    var pos = new Vector2(
                        point.x + offsetX,
                        point.y);
                    factory.Create(monster, pos);
                }
            }
        }

        public class InvalidWaveException: Exception
        {}
    }
}
