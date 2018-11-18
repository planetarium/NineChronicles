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
                Create(poolData);
            }
        }

        public GameObject Create(PoolData data)
        {
            GameObject first = null;
            for (int i = 0; i < data.InitCount; ++i)
            {
                GameObject go = Instantiate(data.Prefab, transform);
                if (first == null)
                {
                    first = go;
                }
                go.name = data.Prefab.name;
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
            PoolData data;
            if (dicts.TryGetValue(name, out data))
            {
                GameObject go = Create(dicts[name]);
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
