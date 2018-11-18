using System.Collections;
using UnityEngine;

namespace Nekoyume.Game
{
    public class StageManager : MonoBehaviour
    {
        private int _currentStage;

        public IEnumerator StartStage(int currentStage)
        {
            _currentStage = currentStage;
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (tables.Stage.TryGetValue(_currentStage, out data))
            {
                var stage = GameObject.Find("Stage").GetComponent<Stage>();
                stage.LoadBackground(data.Background);
                var blind = stage.Blind;
                blind.Show();
                blind.FadeIn(1.0f);
                yield return new WaitForSeconds(1.0f);
                var moveWidget = stage.MoveWidget;
                moveWidget.Show();
                blind.FadeOut(1.0f);
                yield return new WaitForSeconds(1.0f);
                blind.gameObject.SetActive(false);
                // TODO: Load characters
            }

            yield return null;
        }
    }
}