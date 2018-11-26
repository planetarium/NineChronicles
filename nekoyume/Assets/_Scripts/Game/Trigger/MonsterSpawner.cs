using Nekoyume.Game.Character;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Nekoyume.Game.Trigger
{
    public class MonsterSpawner : MonoBehaviour
    {
        private Stage _stage;
        private int _wave = 0;
        private int _monsterPower = 0;

        private void Awake()
        {
            Event.OnEnemyDead.AddListener(OnEnemyDead);
        }

        private void Start()
        {
            _stage = GetComponentInParent<Stage>();
        }

        private void OnEnemyDead()
        {
            if (IsClearWave())
            {
                if (!NextWave())
                {
                    Event.OnStageClear.Invoke();
                }
            }
        }

        public void SetData(int monsterPower)
        {
            _wave = 3;
            _monsterPower = monsterPower;

            NextWave();
        }

        private bool IsClearWave()
        {
            var characters = _stage.GetComponentsInChildren<Character.Base>();
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
            return true;
        }

        private void SpawnWave()
        {
            Factory.EnemyFactory factory = GetComponentInParent<Factory.EnemyFactory>();
            var player = _stage.GetComponentInChildren<Character.Player>();
            int monsterCount = 2;
            for (int i = 0; i < monsterCount; ++i)
            {
                GameObject go = factory.Create("1001");
                go.transform.position = new Vector2(
                    player.transform.position.x + 5.0f + Random.Range(-0.1f, 0.1f),
                    Random.Range(-0.7f, -1.3f));

                var enemy = go.GetComponent<Enemy>();
                enemy.Power = _monsterPower;
            }
        }
    }
}
