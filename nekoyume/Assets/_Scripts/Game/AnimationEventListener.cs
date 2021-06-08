using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Game
{
    public class AnimationEventListener : MonoBehaviour
    {
        public Dictionary<string, System.Action> CallbackMap { get; private set; }
            = new Dictionary<string, System.Action>();

        public bool TryAddCallback(string key, System.Action action)
        {
            if (CallbackMap.ContainsKey(key))
            {
                return false;
            }

            CallbackMap[key] = action;
            return true;
        }

        public void InvokeCallback(string key)
        {
            if (!CallbackMap.ContainsKey(key))
            {
                return;
            }

            CallbackMap[key].Invoke();
        }
    }
}
