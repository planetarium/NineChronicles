using UnityEngine;
using System.Linq;

namespace Nekoyume.Game
{
    public class MonsterSpawner : MonoBehaviour
    {
        private ObjectPool _objectPool;
        private int _stageId = 0;
        private int _monsterPower = 0;

        public void Start()
        {
            _objectPool = GetComponent<ObjectPool>();
        }

        public void Play(int stageId, int monsterPower)
        {
            _stageId = stageId;
            _monsterPower = monsterPower;
            SpawnWave();
        }

        private void SpawnWave()
        {
            int monsterCount = 5;
            for (int i = 0; i < monsterCount; ++i)
            {
                var character = _objectPool.Get<Enemy>();
                character._Load("1001");
            }
        }
    }
}
