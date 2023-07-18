#if UNITY_EDITOR || LIB9C_DEV_EXTENSIONS
using Lib9c.DevExtensions.Model;
using Nekoyume.Game.ScriptableObject;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "TestbedCreateAvatar", menuName = "Scriptable Object/Testbed/CreateAvatar",
        order = int.MaxValue)]
    public class TestbedCreateAvatarScriptableObject : BaseTestbedScriptableObject<TestbedCreateAvatar>
    {
    }
}
#endif
