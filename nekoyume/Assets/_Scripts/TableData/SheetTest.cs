using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nekoyume.TableData
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
        public IEnumerator GetEnumeratorTest()
        {
            var enumerator = _tableSheets.Background.GetEnumerator();
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
