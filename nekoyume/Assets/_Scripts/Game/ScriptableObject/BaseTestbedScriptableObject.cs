using Lib9c.DevExtensions.Model;

namespace Nekoyume.Game.ScriptableObject
{
    public class BaseTestbedScriptableObject<T> : UnityEngine.ScriptableObject where T : BaseTestbedModel
    {
        public T Data;
    }
}
