using System;
using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.Game
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
                Create(poolData.Prefab, poolData.InitCount);
            }
        }

        public GameObject Create(GameObject prefab, int count)
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
            string name = typeof(T).Name;
            List<GameObject> list;
            if (objects.TryGetValue(name, out list))
            {
                foreach (GameObject go in list)
                {
                    if (go.activeSelf)
                        continue;

                    go.SetActive(true);
                    return go.GetComponent<T>();
                }
            }
            PoolData poolData;
            if (dicts.TryGetValue(name, out poolData))
            {
                GameObject go = Create(poolData.Prefab, poolData.AddCount);
                go.SetActive(true);
                return go.GetComponent<T>();
            }
            return null;
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
    }
}
