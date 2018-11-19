using System.Collections;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

namespace Nekoyume.Game
{
    public class StageManager : MonoBehaviour
    {
        private int _currentStage;

        public ObjectPool ObjectPool { get; private set; }
        private MonsterSpawner _monsterSpawner;

        private void Awake()
        {
            ObjectPool = gameObject.AddComponent<ObjectPool>();
            var character = new PoolData
            {
                Prefab = Resources.Load<GameObject>("Prefab/Character"),
                AddCount = 5,
                InitCount = 10
            };
            ObjectPool.list.Add(character);
            _monsterSpawner = gameObject.AddComponent<MonsterSpawner>();
        }

        public IEnumerator StartStage(Avatar avatar)
        {
            _currentStage = avatar.world_stage;
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (tables.Stage.TryGetValue(_currentStage, out data))
            {
                var gameScript = this.GetRootComponent<Game>();
                var stage = GameObject.Find("Stage").GetComponent<Stage>();
                var blind = gameScript.Blind;
                blind.Show();
                blind.FadeIn(1.0f);
                yield return new WaitForSeconds(1.0f);

                var moveWidget = gameScript.MoveWidget;
                moveWidget.Show();
                ObjectPool.ReleaseAll();
                stage.LoadBackground(data.Background);

                var character = ObjectPool.Get<Character>();
                character._Load(avatar);
                
                blind.FadeOut(1.0f);
                yield return new WaitForSeconds(1.0f);
                blind.gameObject.SetActive(false);

                _monsterSpawner.Play(_currentStage, data.MonsterPower);
            }

            yield return null;
        }
    }
}