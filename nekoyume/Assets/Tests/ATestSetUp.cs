using System.Collections;
using Nekoyume;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class ATestSetUp
    {
        public static MonoBehaviour monoBehaviour;
        public static TableSheets tableSheets;
        
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Debug.LogWarning("ATestSetUp.SetUp() called.");
            monoBehaviour = new GameObject().AddComponent<SimpleMonoBehaviour>();
            tableSheets = new TableSheets();
            yield return monoBehaviour.StartCoroutine(tableSheets.CoInitialize());
            Debug.LogWarning("ATestSetUp.SetUp() end.");
        }

        [UnityTest]
        public IEnumerator ValidateTableSheets()
        {
            Assert.IsNotNull(tableSheets);
            
            yield break;
        }
    }
}
