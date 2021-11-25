#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions.Model;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "TestbedSell", menuName = "Scriptable Object/Testbed/sell",
        order = int.MaxValue)]
    public class TestbedToolScriptableObject : ScriptableObject
    {
        public TestbedSell data;

        // public void OnValidate() {
        //     Debug.Log(Time.time);
        // }
    }
}
#endif
