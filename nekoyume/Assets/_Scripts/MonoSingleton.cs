using UnityEngine;

namespace Nekoyume
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        public static T instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.Log(
                        $"[MonoSingleton]Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }

                lock (Lock)
                {
                    if (_instance != null) return _instance;

                    _instance = (T) FindObjectOfType(typeof(T));

                    if (_instance == null)
                    {
                        _instance = new GameObject(typeof(T).ToString(), typeof(T)).GetComponent<T>();

                        Debug.Log(_instance == null
                            ? $"[MonoSingleton]Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it."
                            : $"[MonoSingleton]An instance of {typeof(T)} is needed in the scene, so '{_instance.name}' was created with DontDestroyOnLoad.");
                    }

                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        Debug.Log(
                            $"[MonoSingleton]Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                    }

                    return _instance;
                }
            }
        }

        private static readonly object Lock = new object();
        private static bool _applicationIsQuitting = false;

        #region Mono

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = (T) this;

                name = typeof(T).ToString();

                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.Log($"{typeof(T)} already exist!");

                Destroy(gameObject);
            }
        }

        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// In principle, a Singleton is only destroyed when application quits.
        /// If any script calls Instance after it have been destroyed, 
        ///   it will create a buggy ghost object that will stay on the Editor scene
        ///   even after stopping playing the Application. Really bad!
        /// So, this was made to be sure we're not creating that buggy ghost object.
        /// </summary>
        protected virtual void OnDestroy()
        {
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        #endregion
    }
}
