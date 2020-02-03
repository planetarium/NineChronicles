using System.Collections;
using Nekoyume;
using Nekoyume.Game;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class SheetTest
    {
        private MonoBehaviour _monoBehaviour;
        private TableSheets _tableSheets;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _monoBehaviour = new GameObject().AddComponent<SimpleMonoBehaviour>();
            _tableSheets = new TableSheets();
            var sheets = Game.GetTableCsvAssets();
            foreach (var sheet in sheets)
            {
                _tableSheets.SetToSheet(sheet.Key, sheet.Value);
            }

            _tableSheets.ItemSheetInitialize();
            _tableSheets.QuestSheetInitialize();
            yield break;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_monoBehaviour.gameObject);
            _tableSheets = null;
            yield break;
        }

        [UnityTest]
        public IEnumerator GetEnumeratorTest()
        {
            var enumerator = _tableSheets.StageSheet.GetEnumerator();
            int previousKey;

            if (enumerator.MoveNext())
            {
                previousKey = enumerator.Current.Key;
            }
            else
            {
                yield break;
            }

            while (enumerator.MoveNext())
            {
                var currentKey = enumerator.Current.Key;
                Assert.Greater(currentKey, previousKey);
                previousKey = currentKey;
            }

            enumerator.Dispose();
        }
    }
}
