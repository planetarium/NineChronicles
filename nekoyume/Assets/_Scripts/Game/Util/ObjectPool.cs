using System;
using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.Game.Util
{
    [Serializable]
    public struct PoolData
    {
        public GameObject Prefab;
        public int InitCount;
        public int AddCount;
    }

    public class ObjectPool : MonoBehaviour
    {
        public List<PoolData> list = new List<PoolData>();

        private Dictionary<string, List<GameObject>> objects = new Dictionary<string, List<GameObject>>();
        private Dictionary<string, PoolData> dicts = new Dictionary<string, PoolData>();

        public void Start()
        {
            foreach (var poolData in list)
            {
                dicts.Add(poolData.Prefab.name, poolData);
                Add(poolData.Prefab, poolData.InitCount);
            }
        }

        private GameObject Add(GameObject prefab, int count)
        {
            GameObject first = null;
            for (int i = 0; i < count; ++i)
            {
                GameObject go = Instantiate(prefab, transform);
                if (first == null)
                {
                    first = go;
                }
                go.name = prefab.name;
                go.SetActive(false);
                List<GameObject> list;
                if (!objects.TryGetValue(go.name, out list))
                {
                    list = new List<GameObject>();
                    objects.Add(go.name, list);
                }
                list.Add(go);
            }

            return first;
        }

        public T Get<T>() where T : MonoBehaviour
        {
            return Get<T>(Vector3.zero);
        }

        public T Get<T>(Vector3 position) where T : MonoBehaviour
        {
            string name = typeof(T).Name;
            List<GameObject> list;
            if (objects.TryGetValue(name, out list))
            {
                foreach (GameObject go in list)
                {
                    if (go.activeSelf)
                        continue;

                    go.transform.position = position;
                    go.SetActive(true);
                    return go.GetComponent<T>();
                }
            }
            PoolData poolData;
            if (dicts.TryGetValue(name, out poolData))
            {
                GameObject go = Add(poolData.Prefab, poolData.AddCount);
                go.transform.position = position;
                go.SetActive(true);
                return go.GetComponent<T>();
            }
            return null;
        }

        public bool Remove<T>(GameObject go)
        {
            string name = typeof(T).Name;
            List<GameObject> list;
            if (objects.TryGetValue(name, out list))
            {
                Destroy(go);
                return list.Remove(go);
            }
            return false;
        }

        public void ReleaseAll()
        {
            foreach (var pair in objects)
            {
                List<GameObject> list = pair.Value;
                foreach (GameObject go in list)
                {
                    go.SetActive(false);
                }
            }
        }

        public GameObject Get(string objName, bool create, Vector3 position = default(Vector3))
        {
            List<GameObject> gameObjects;
            if (objects.TryGetValue(objName, out gameObjects))
            {
                foreach (var go in gameObjects)
                {
                    if (go.activeSelf)
                        continue;

                    go.transform.position = position;
                    go.SetActive(true);
                    return go;
                }
            }
            return create ? Create(objName, position) : null;

        }

        public GameObject Create(string objName, Vector3 position)
        {
            PoolData poolData;
            if (dicts.TryGetValue(objName, out poolData))
            {
                GameObject go = Add(poolData.Prefab, poolData.AddCount);
                go.transform.position = position;
                go.SetActive(true);
                return go;
            }
            throw new NullReferenceException($"Set `{objName}` first in ObjectPool.");
        }
    }
}
