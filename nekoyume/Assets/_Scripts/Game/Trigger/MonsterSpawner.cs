using Nekoyume.Game.Character;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Nekoyume.Game.Trigger
{
    public class MonsterSpawner : MonoBehaviour
    {
        private int _monsterPower = 0;

        public void SetData(int monsterPower)
        {
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
                var player = other.gameObject.GetComponent<Player>(); 
                player.Walkable = false;
                SpawnWave(player);
            }
        }

        private void SpawnWave(Player player)
        {
            Factory.EnemyFactory factory = GetComponentInParent<Factory.EnemyFactory>();
            int monsterCount = 2;
            for (int i = 0; i < monsterCount; ++i)
            {
                GameObject go = factory.Create("1001");

                go.transform.position = new Vector2(
                    transform.position.x + Random.Range(-0.1f, 0.1f), Random.Range(-0.7f, -1.3f));
                player.Targets.Add(go);
                var enemy = go.GetComponent<Enemy>();
                enemy.Power = _monsterPower;
                enemy.Targets.Add(player.gameObject);
            }
        }
    }
}
