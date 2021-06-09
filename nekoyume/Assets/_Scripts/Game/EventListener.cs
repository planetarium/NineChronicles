using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Game
{
    public class EventListener : MonoBehaviour
    {
        public Dictionary<string, List<System.Action>> CallbackMap { get; private set; }
            = new Dictionary<string, List<System.Action>>();

        public void AddEvent(string key, System.Action action)
        {
            if (!CallbackMap.ContainsKey(key))
            {
                CallbackMap[key] = new List<System.Action>();
            }

            CallbackMap[key].Add(action);
        }

        /// <summary>
        /// Raises event with parameter. (Can be used in unity animation event.)
        /// </summary>
        /// <param name="key">
        /// Event key to raise.
        /// </param>
        public void RaiseEvent(string key)
        {
            if (!CallbackMap.ContainsKey(key))
            {
                return;
            }

            foreach (var action in CallbackMap[key])
            {
                action.Invoke();
            }
        }
    }
}
