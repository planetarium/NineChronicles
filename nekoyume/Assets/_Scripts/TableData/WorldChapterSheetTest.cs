using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nekoyume.TableData
{
    public class WorldChapterSheetTest
    {
        private MonoBehaviour _monoBehaviour;
        private TableSheets _tableSheets;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _monoBehaviour = new GameObject().AddComponent<SimpleMonoBehaviour>();
            _tableSheets = new TableSheets();
            yield return _monoBehaviour.StartCoroutine(_tableSheets.CoInitialize());
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_monoBehaviour.gameObject);
            _tableSheets = null;
            yield break;
        }

        [UnityTest]
        public IEnumerator TryGetByStageTest()
        {
            var lastStageId = _tableSheets.StageSheet.ToOrderedList().Last().Stage;
            var testStageId = lastStageId + 1;
            var success = _tableSheets.WorldChapterSheet.TryGetByStage(testStageId, out var row);
            Assert.IsTrue(success);
            Assert.IsNotNull(row);
            Assert.GreaterOrEqual(lastStageId, row.StageBegin);
            Assert.LessOrEqual(lastStageId, row.StageEnd);

            yield break;
        }
    }
}
