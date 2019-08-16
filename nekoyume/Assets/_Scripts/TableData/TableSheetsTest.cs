using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nekoyume.TableData
{
    public class TableSheetsTest
    {
        private MonoBehaviour _monoBehaviour;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _monoBehaviour = new GameObject().AddComponent<SimpleMonoBehaviour>();
            yield break;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_monoBehaviour.gameObject);
            yield break;
        }

        [UnityTest]
        public IEnumerator InitializeTest()
        {
            var tableSheets = new TableSheets();
            yield return _monoBehaviour.StartCoroutine(tableSheets.CoInitialize());
            Assert.AreEqual(tableSheets.loadProgress.Value, 1f);  
            Assert.NotNull(tableSheets.Background);
        }
    }
}
