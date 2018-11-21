using DG.Tweening;
using UnityEngine;


namespace Nekoyume.Game.Trigger
{
    public class MonsterSpawner : MonoBehaviour
    {
        private int _stageId = 0;
        private int _monsterPower = 0;

        public void SetData(int stageId, int monsterPower)
        {
            _stageId = stageId;
            _monsterPower = monsterPower;

            Collider2D collider = GetComponent<Collider2D>();
            collider.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log(other);
            if (other.gameObject.name == "Player")
            {
                Collider2D collider = GetComponent<Collider2D>();
                collider.enabled = false;

                SpawnWave();
            }
        }

        private void SpawnWave()
        {
            Factory.EnemyFactory factory = GetComponentInParent<Factory.EnemyFactory>();
            int monsterCount = 5;
            for (int i = 0; i < monsterCount; ++i)
            {
                GameObject go = factory.Create("1001");

                go.transform.position = new Vector2(
                    transform.position.x + Random.Range(-0.1f, 0.1f), Random.Range(-0.7f, -1.3f));
            }
        }
    }
}
