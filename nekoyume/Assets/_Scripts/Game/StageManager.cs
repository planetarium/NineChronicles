using System.Collections;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

namespace Nekoyume.Game
{
    public class StageManager : MonoBehaviour
    {
        private int _currentStage;

        public IEnumerator StartStage(Avatar avatar)
        {
            _currentStage = avatar.world_stage;
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (tables.Stage.TryGetValue(_currentStage, out data))
            {
                var stage = GameObject.Find("Stage").GetComponent<Stage>();
                var blind = stage.Blind;
                blind.Show();
                blind.FadeIn(1.0f);
                yield return new WaitForSeconds(1.0f);

                var moveWidget = stage.MoveWidget;
                moveWidget.Show();
                var objectPool = stage.ObjectPool;
                objectPool.ReleaseAll();
                stage.LoadBackground(data.Background);

                var character = objectPool.Get<Character>();
                character._Load(avatar);
                
                blind.FadeOut(1.0f);
                yield return new WaitForSeconds(1.0f);
                blind.gameObject.SetActive(false);

                var monsterSpawner = stage.MonsterSpawner;
                monsterSpawner.Play(_currentStage, data.MonsterPower);
            }

            yield return null;
        }
    }
}