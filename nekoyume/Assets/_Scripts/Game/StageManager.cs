using System.Collections;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

namespace Nekoyume.Game
{
    public class StageManager : MonoBehaviour
    {
        private int _currentStage;
        private ObjectPool _objectPool;
        private MonsterSpawner _monsterSpawner;
        private Game _game;

        private void Awake()
        {
            _objectPool = gameObject.AddComponent<ObjectPool>();
            var character = new PoolData
            {
                Prefab = Resources.Load<GameObject>("Prefab/Character"),
                AddCount = 5,
                InitCount = 10
            };
            _objectPool.list.Add(character);
            _monsterSpawner = gameObject.AddComponent<MonsterSpawner>();
            _game = this.GetRootComponent<Game>();
        }

        public IEnumerator WorldEntering()
        {
            _currentStage = _game.Avatar.world_stage;
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (tables.Stage.TryGetValue(_currentStage, out data))
            {
                var gameScript = this.GetRootComponent<Game>();
                var stage = GameObject.Find("Stage").GetComponent<Stage>();
                var blind = gameScript.Blind;
                stage.Id = _currentStage;
                blind.Show();
                blind.FadeIn(1.0f);
                yield return new WaitForSeconds(1.0f);

                var moveWidget = gameScript.MoveWidget;
                moveWidget.Show();
                _objectPool.ReleaseAll();
                stage.LoadBackground(data.Background);

                var character = _objectPool.Get<Character>();
                character._Load(_game.Avatar);
                
                blind.FadeOut(1.0f);
                yield return new WaitForSeconds(1.0f);
                blind.gameObject.SetActive(false);

                Event.OnStageStart.Invoke();
                _monsterSpawner.Play(_currentStage, data.MonsterPower);
            }
        }

        public IEnumerator RoomEntering()
        {
            var stage = GameObject.Find("Stage").GetComponent<Stage>();
            var blind = _game.Blind;
            var moveWidget = _game.MoveWidget;
            blind.Show();
            blind.FadeIn(1.0f);
            yield return new WaitForSeconds(1.0f);
            moveWidget.Show();
            stage.Id = 0;
            stage.LoadBackground("room");
            var character = _objectPool.Get<Character>();
            character._Load(_game.Avatar);
            blind.FadeOut(1.0f);
            yield return new WaitForSeconds(1.0f);
            blind.gameObject.SetActive(false);
            Event.OnStageStart.Invoke();
        }
    }
}