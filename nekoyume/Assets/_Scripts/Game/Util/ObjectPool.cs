using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Nekoyume.Game.Util
{
    public interface IObjectPool
    {
        GameObject Add(GameObject prefab, int count);

        T Get<T>(Vector3 position) where T : MonoBehaviour;

        bool Remove<T>(GameObject go);

        void ReleaseAll();

        GameObject Get(string objName, bool create, Vector3 position = default);
    }

    [Serializable]
    public struct PoolData
    {
        public GameObject Prefab;
        public int InitCount;
        public int AddCount;
    }

    public class ObjectPool : MonoBehaviour, IObjectPool
    {
        public class Impl : IObjectPool
        {
            private Transform _parent;

            private readonly Dictionary<string, PoolData> _dict =
                new Dictionary<string, PoolData>();

            private readonly Dictionary<string, List<GameObject>> _objects =
                new Dictionary<string, List<GameObject>>();

            public Impl(Transform parent, IEnumerable<PoolData> list)
            {
                if (parent is null)
                {
                    throw new ArgumentNullException(nameof(parent));
                }

                _parent = parent;

                if (list is null)
                {
                    return;
                }

                foreach (var poolData in list)
                {
                    _dict.Add(poolData.Prefab.name, poolData);
                    Add(poolData.Prefab, poolData.InitCount);
                }
            }

            public GameObject Add(GameObject prefab, int count)
            {
                GameObject first = null;
                for (int i = 0; i < count; ++i)
                {
                    var go = Instantiate(prefab, _parent);
                    if (!first)
                    {
                        first = go;
                    }

                    go.name = prefab.name;
                    go.SetActive(false);
                    if (!_objects.TryGetValue(go.name, out var list))
                    {
                        list = new List<GameObject>();
                        _objects.Add(go.name, list);
                    }

                    list.Add(go);
                }

                return first;
            }

            public T Get<T>(Vector3 position) where T : MonoBehaviour
            {
                var name = typeof(T).Name;
                if (_objects.TryGetValue(name, out var list))
                {
                    foreach (var go in list.Where(go => !go.activeSelf))
                    {
                        go.transform.position = position;
                        go.SetActive(true);
                        return go.GetComponent<T>();
                    }
                }

                if (_dict.TryGetValue(name, out var poolData))
                {
                    var go = Add(poolData.Prefab, poolData.AddCount);
                    go.transform.position = position;
                    go.SetActive(true);
                    return go.GetComponent<T>();
                }

                return null;
            }

            public bool Remove<T>(GameObject go)
            {
                var key = typeof(T).Name;
                if (!_objects.TryGetValue(key, out var gameObjects))
                {
                    return false;
                }

                Destroy(go);
                return gameObjects.Remove(go);
            }

            public void ReleaseAll()
            {
                foreach (var go in _objects
                    .Select(pair => pair.Value)
                    .SelectMany(l => l.Where(go => go != null)))
                {
                    go.SetActive(false);
                }
            }

            public void ReleaseExcept(IEnumerable<GameObject> whiteList)
            {
                foreach (var go in _objects
                    .Select(pair => pair.Value)
                    .SelectMany(l =>
                        l.Where(o => o != null && !whiteList.Contains(o))))
                {
                    go.SetActive(false);
                }
            }

            public GameObject Get(string objName, bool create, Vector3 position = default(Vector3))
            {
                if (_objects.TryGetValue(objName, out var gameObjects))
                {
                    foreach (var go in gameObjects.Where(go => !go.activeSelf))
                    {
                        go.transform.position = position;
                        go.SetActive(true);
                        return go;
                    }
                }

                return create
                    ? Create(objName, position)
                    : null;
            }

            private GameObject Create(string objName, Vector3 position)
            {
                var go = _dict.TryGetValue(objName, out var poolData)
                    ? Add(poolData.Prefab, poolData.AddCount)
                    : Add(objName);

                if (!go)
                {
                    throw new NullReferenceException($"Set `{objName}` first in ObjectPool.");
                }

                go.transform.position = position;
                go.SetActive(true);
                return go;
            }

            private GameObject Add(string prefabName)
            {
                if (!_objects.TryGetValue(prefabName, out var objectsList))
                {
                    return null;
                }

                var go = Instantiate(objectsList.First(), _parent);
                go.name = prefabName;
                go.SetActive(false);
                objectsList.Add(go);
                return go;
            }
        }

        private Impl _impl;

        public List<PoolData> list = new List<PoolData>();

        public void Initialize()
        {
            _impl = new Impl(transform, list);
        }

        public GameObject Add(GameObject prefab, int count)
        {
            return _impl.Add(prefab, count);
        }

        public T Get<T>(Vector3 position = default) where T : MonoBehaviour
        {
            return _impl.Get<T>(position);
        }

        public bool Remove<T>(GameObject go)
        {
            return _impl.Remove<T>(go);
        }

        public void ReleaseAll()
        {
            _impl.ReleaseAll();
        }

        public void ReleaseExcept(IEnumerable<GameObject> whiteList)
        {
            _impl.ReleaseExcept(whiteList);
        }

        public GameObject Get(string objName, bool create, Vector3 position = default)
        {
            return _impl.Get(objName, create, position);
        }
    }
}
