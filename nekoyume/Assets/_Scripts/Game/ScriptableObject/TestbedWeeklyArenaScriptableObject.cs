#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions.Model;
using Nekoyume.Game.ScriptableObject;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "TestbedWeeklyArena", menuName = "Scriptable Object/Testbed/WeeklyArena",
        order = int.MaxValue)]
    public class TestbedWeeklyArenaScriptableObject : BaseTestbedScriptableObject<TestbedWeeklyArena>
    {
    }
}
#endif
