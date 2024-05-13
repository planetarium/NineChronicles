#nullable enable

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Nekoyume
{
    public class ResourceManager
    {
        public static string BattleLabel => "Battle";
        public static string CharacterLabel => "Character";

        private static class Singleton
        {
            internal static readonly ResourceManager Value = new();
        }

        public static ResourceManager Instance => Singleton.Value;

        /// <summary>
        /// 프로세스 종료시까지 유지되는 리소스
        /// </summary>
        private readonly Dictionary<string, Object> _dontDestroyOnLoadResources = new();

        /// <summary>
        /// 씬 전환시 제거되는 리소스
        /// </summary>
        private readonly Dictionary<string, Object> _resources = new();

        private ResourceManager() { }

        public async UniTask InitializeAsync()
        {
            await Addressables.InitializeAsync();

            _dontDestroyOnLoadResources.Clear();
            _resources.Clear();
        }

        public T? Load<T>(string key) where T : Object
        {
            if (_resources.TryGetValue(key, out Object resource))
                return resource as T;

            if (_dontDestroyOnLoadResources.TryGetValue(key, out resource))
                return resource as T;

            return null;
        }

        public GameObject? Instantiate(string key, Transform? parent = null)
        {
            var prefab = Load<GameObject>($"{key}");
            if (prefab == null)
            {
                NcDebug.LogError($"Failed to load prefab : {key}");
                return null;
            }

            var isDontDestroy = _dontDestroyOnLoadResources.ContainsKey(key);
            var go = Object.Instantiate(prefab, parent);
            go.name = prefab.name;

            if (isDontDestroy)
                Object.DontDestroyOnLoad(go);

            // TODO: Pooling
            return go;
        }

        public void Destroy(GameObject? go)
        {
            if (go == null)
                return;

            // TODO: Pooling
            Object.Destroy(go);
        }

#region Addressable
        public async UniTask LoadAsync<T>(string key, bool isDonDestroy = false) where T : Object
        {
            if (_resources.ContainsKey(key))
                return;

            if (_dontDestroyOnLoadResources.ContainsKey(key))
                return;

            var asyncOperation = Addressables.LoadAssetAsync<T>(key);
            await asyncOperation;

            if (isDonDestroy)
                _dontDestroyOnLoadResources.Add(key, asyncOperation.Result);
            else
                _resources.Add(key, asyncOperation.Result);
        }

        public async UniTask LoadAllAsync<T>(string label, bool isDonDestroy = false) where T : Object
        {
            var opHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
            await opHandle;

            NcDebug.Log($"LoadAllAsync : {label}");

            foreach (var result in opHandle.Result)
                await LoadAsync<T>(result.PrimaryKey, isDonDestroy);
        }

        public void Release(string key)
        {
            if (!_resources.TryGetValue(key, out var resource)) return;
            Addressables.Release(resource);
            _resources.Remove(key);
        }

        public void ReleaseAll()
        {
            foreach (var resource in _resources)
                Addressables.Release(resource.Value);

            _resources.Clear();
        }
#endregion Addressable
    }
}
