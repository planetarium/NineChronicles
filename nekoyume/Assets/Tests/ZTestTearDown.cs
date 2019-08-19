using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

namespace Tests
{
    public class ZTestTearDown
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.LogWarning("ZTestTearDown.TearDown() called.");
            
            if (ATestSetUp.monoBehaviour)
            {
                Object.Destroy(ATestSetUp.monoBehaviour.gameObject);    
            }

            if (ATestSetUp.tableSheets is null)
            {
                yield break;
            }
            
            ATestSetUp.tableSheets = null;
        }
        
        [UnityTest]
        public IEnumerator ValidateTableSheets()
        {
            Assert.IsNotNull(ATestSetUp.tableSheets);
            
            yield break;
        }
    }
}
