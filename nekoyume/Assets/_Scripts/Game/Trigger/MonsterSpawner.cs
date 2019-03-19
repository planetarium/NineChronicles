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
        private readonly float[,] _spawnPoints =
        {
            {0.9f, -0.8f},
            {0.8f, -1.4f},
            {0.9f, -0.7f},
            {0.8f, -1.0f},
            {0.9f, -1.2f}
        };

        private Monster _monster;
        private int _monsterPower;

        private Stage _stage;
        private int _stageId;
        private int _wave;

        private void Awake()
        {
            Event.OnEnemyDead.AddListener(OnEnemyDead);
        }

        private void Start()
        {
            _stage = GetComponentInParent<Stage>();
        }

        private void OnEnemyDead(Enemy _)
        {
            if (IsClearWave())
                if (!NextWave())
                    Event.OnStageClear.Invoke();
        }

        public void SetData(int stageId, Monster monster)
        {
            _stageId = stageId;
            _monster = monster;
            SpawnWave();
        }

        public void SetData(int stageId, int monsterPower)
        {
            _stageId = stageId;
            _wave = 3;
            _monsterPower = monsterPower;

            NextWave();
        }

        private bool IsClearWave()
        {
            var characters = _stage.GetComponentsInChildren<Character.CharacterBase>();
            foreach (var character in characters)
            {
                if (character.tag == Tag.Player)
                    continue;

                if (character.gameObject.activeSelf)
                    return false;
            }

            return true;
        }

        private bool NextWave()
        {
            if (_wave <= 0)
                return false;

            _wave--;
            SpawnWave();
            if (_wave == 0 && HasBoss()) SpawnBoss();
            return true;
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
                    _spawnPoints[r, 0] + offsetX,
                    _spawnPoints[r, 1]);
                factory.Create(_monster, pos);
            }
        }

        private bool HasBoss()
        {
            var tables = this.GetRootComponent<Tables>();
            var stageData = tables.Stage[_stageId];
            return stageData.bossId > 0;
        }

        private void SpawnBoss()
        {
            var tables = this.GetRootComponent<Tables>();
            var stageData = tables.Stage[_stageId];

            var factory = GetComponentInParent<EnemyFactory>();
            var player = _stage.GetComponentInChildren<Character.Player>();
            var offsetX = player.transform.position.x + 5.5f;
            factory.CreateBoss(stageData.bossId, new Vector2(offsetX, -1.0f), _monsterPower);
        }
    }
}
