using Nekoyume;
using Nekoyume.UI;
using System;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NineChronicles.MOD
{
    public class ModManager : MonoBehaviour
    {
        private class ModInfo
        {
            public Type Type;
            public IMod Mod;
        }

        public static ModManager Instance { get; private set; }

        private ModInfo[] _mods;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (!Application.isEditor && Platform.IsMobilePlatform())
            {
                Debug.Log("ModManager.Initialize() is skipped because it is a mobile platform.");
                return;
            }

            Debug.Log("ModManager.Initialize()");
            var go = new GameObject("ModManager");
            go.AddComponent<ModManager>();
            DontDestroyOnLoad(go);
        }

        #region MonoBehaviour
        private void Awake()
        {
            Debug.Log("ModManager.Awake()");

            if (Instance != null)
            {
                Debug.LogWarning("ModManager.Instance is already set. Destroy this instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeMods();
        }

        private void Update()
        {
            if (!CheckInput())
            {
                return;
            }

            var arenaBoard = Widget.Find<ArenaBoard>();
            if (!arenaBoard ||
                !arenaBoard.IsActive())
            {
                return;
            }

            var firstMod = _mods.FirstOrDefault();
            if (firstMod.Mod == null)
            {
                Debug.Log("Create instance of the first mod.");
                firstMod.Mod = (IMod)Activator.CreateInstance(firstMod.Type);
                firstMod.Mod.Initialize();
            }

            firstMod.Mod.Show();
        }
        #endregion MonoBehaviour

        private void InitializeMods()
        {
            var modType = typeof(IMod);
            _mods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => modType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .Select(x => new ModInfo { Type = x })
                .ToArray();
        }

        private bool CheckInput()
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) &&
                (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                Input.GetKeyDown(KeyCode.Space))
            {
                if (Application.platform == RuntimePlatform.OSXEditor ||
                    Application.platform == RuntimePlatform.OSXPlayer)
                {
                    if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
                    {
                        return true;
                    }
                }
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
