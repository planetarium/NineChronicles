using System.Linq;
using Nekoyume.Game.Character;
using UnityEngine;


namespace Nekoyume.Game.Trigger
{
    public class MonsterSpawner : MonoBehaviour
    {
        private Stage _stage;
        private int _stageId;
        private int _wave = 0;
        private int _monsterPower = 0;
        private float[,] _spawnPoints = new [,] {
            {0.0f, -0.8f},
            {0.1f, -1.0f},
            {0.2f, -1.2f},
            {0.4f, -0.9f},
            {0.5f, -1.1f},
            {0.4f, -1.4f},
            {0.9f, -0.7f},
            {0.8f, -1.0f},
            {0.9f, -1.2f}};

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
            {
                if (!NextWave())
                {
                    Event.OnStageClear.Invoke();
                }
            }
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
            if (_wave == 0 && HasBoss())
            {
                SpawnBoss();
            }
            return true;
        }

        private void SpawnWave()
        {
            var selector = new Util.WeightedSelector<Data.Table.MonsterAppear>();
            var tables = this.GetRootComponent<Data.Tables>();
            foreach (var appearPair in tables.MonsterAppear)
            {
                Data.Table.MonsterAppear appearData = appearPair.Value;
                if (_stageId > appearData.StageMax)
                    continue;

                if (appearData.Weight <= 0)
                    continue;

                selector.Add(appearData, appearData.Weight);
            }

            Factory.EnemyFactory factory = GetComponentInParent<Factory.EnemyFactory>();
            var player = _stage.GetComponentInChildren<Character.Player>();
            float offsetX = player.transform.position.x + 5.5f;
            int[] randIndex = Enumerable.Range(0, _spawnPoints.Length / 2).ToArray()
                                        .OrderBy(n => System.Guid.NewGuid()).ToArray();
            int monsterCount = 2;
            for (int i = 0; i < monsterCount; ++i)
            {
                int r = randIndex[i];
                Vector2 pos = new Vector2(
                    _spawnPoints[r, 0] + offsetX,
                    _spawnPoints[r, 1]);

                Data.Table.MonsterAppear appearData = selector.Select();
                factory.Create(appearData.MonsterId, pos, _monsterPower);
            }
        }

        private bool HasBoss()
        {
            var tables = this.GetRootComponent<Data.Tables>();
            var stageData = tables.Stage[_stageId];
            return stageData.bossId > 0;
        }

        private void SpawnBoss()
        {
            var tables = this.GetRootComponent<Data.Tables>();
            var stageData = tables.Stage[_stageId];

            Factory.EnemyFactory factory = GetComponentInParent<Factory.EnemyFactory>();
            var player = _stage.GetComponentInChildren<Character.Player>();
            float offsetX = player.transform.position.x + 5.5f;
            factory.Create(stageData.bossId, new Vector2(offsetX, -1.0f), _monsterPower);
        }
    }
}
