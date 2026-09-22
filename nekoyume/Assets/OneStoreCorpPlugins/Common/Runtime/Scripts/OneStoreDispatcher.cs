using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;

namespace OneStore.Common
{
    public class OneStoreDispatcher : MonoBehaviour
    {
        static OneStoreDispatcher _instance;
        static volatile bool _queued = false;
        static List<Action> _backlog = new List<Action>(8);
        static List<Action> _actions = new List<Action>(8);

        public static void RunAsync(Action action) {
            ThreadPool.QueueUserWorkItem(o => action());
        }
 
        public static void RunAsync(Action<object> action, object state) {
            ThreadPool.QueueUserWorkItem(o => action(o), state);
        }
    
        public static void RunOnMainThread(Action action)
        {
            lock(_backlog) {
                _backlog.Add(action);
                _queued = true;
            }
        }
    
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if(_instance == null) {
                _instance = new GameObject("OneStoreDispatcher").AddComponent<OneStoreDispatcher>();
                // Hide the object so that it it won't be accessible (to make sure it won't be deleted accidentally).
                _instance.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                DontDestroyOnLoad(_instance.gameObject);
            }
        }
    
        private void Update()
        {
            if(_queued)
            {
                lock(_backlog) {
                    var tmp = _actions;
                    _actions = _backlog;
                    _backlog = tmp;
                    _queued = false;
                }
    
                foreach(var action in _actions)
                    action();
    
                _actions.Clear();
            }
        }
    }
}

