using UnityEngine;

namespace Nekoyume.Pattern
{
    /// <summary>
    /// ToDo. 루트 게임오브젝트가 아닐 경우 `DontDestroyOnLoad(gameObject);`가 작동하지 않는다.
    /// 특정 게임오브젝트의 자식으로 존재하는 싱글턴 게임오브젝트가 필요할까?
    /// 부모 게임오브젝트가 파괴되었을 때 싱글턴 게임오브젝트는 어떤 형태로 존재해야 하는가?
    /// 필요하다면, 어떻게 제공할까?
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        private static readonly object Lock = new object();
        private static bool _applicationIsQuitting;

        protected virtual bool ShouldRename => false;

        public static T instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    NcDebug.Log(
                        $"[MonoSingleton]Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return _instance;
                }

                lock (Lock)
                {
                    if (_instance)
                        return _instance;

                    _instance = (T) FindObjectOfType(typeof(T));

                    if (!_instance)
                    {
                        _instance = new GameObject(typeof(T).ToString(), typeof(T)).GetComponent<T>();
                        if (!_instance)
                        {
                            NcDebug.LogError("[MonoSingleton]Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                        }

                        NcDebug.Log($"[MonoSingleton]An instance of {typeof(T)} is needed in the scene, so '{_instance.name}' was created with DontDestroyOnLoad.");
                    }

                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        NcDebug.LogError(
                            $"[MonoSingleton]Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                    }

                    return _instance;
                }
            }
        }

        #region Mono

        protected virtual void Awake()
        {
            if (_instance &&
                _instance != this)
            {
                NcDebug.LogWarning($"{typeof(T)} already exist!");
                Destroy(gameObject);
                return;
            }

            if (!_instance)
            {
                _instance = (T) this;
            }

            if (ShouldRename)
            {
                name = typeof(T).ToString();
            }

            DontDestroyOnLoad(gameObject);
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

        // instance를 생성하기 위한 빈 메소드.
        public void EmptyMethod()
        {
        }
    }
}
