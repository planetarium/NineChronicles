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

        public IEnumerator WorldEntering(Stage stage)
        {
            _currentStage = _game.Avatar.world_stage;
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (tables.Stage.TryGetValue(_currentStage, out data))
            {
                var blind = UI.Widget.Find<UI.Blind>();
                yield return StartCoroutine(blind.FadeIn(1.0f, $"STAGE {_currentStage}"));

                UI.Widget.Find<UI.Move>().ShowWorld();
                _objectPool.ReleaseAll();
                stage.Id = _currentStage;
                stage.LoadBackground(data.Background);

                var character = _objectPool.Get<Character>();
                character._Load(_game.Avatar);

                yield return new WaitForSeconds(2.0f);
                yield return StartCoroutine(blind.FadeOut(1.0f));

                Event.OnStageStart.Invoke();
                _monsterSpawner.Play(_currentStage, data.MonsterPower);
            }
        }

        public IEnumerator RoomEntering(Stage stage)
        {
            var blind = UI.Widget.Find<UI.Blind>();
            yield return StartCoroutine(blind.FadeIn(1.0f, "ROOM"));

            UI.Widget.Find<UI.Move>().ShowRoom();
            stage.Id = 0;
            stage.LoadBackground("room");
            var character = _objectPool.Get<Character>();
            character._Load(_game.Avatar);

            yield return new WaitForSeconds(2.0f);
            yield return StartCoroutine(blind.FadeOut(1.0f));

            Event.OnStageStart.Invoke();
        }
    }
}
