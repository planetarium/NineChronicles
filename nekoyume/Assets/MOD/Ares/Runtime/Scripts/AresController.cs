using NineChronicles.MOD.Ares.UI;
using UnityEngine;

namespace NineChronicles.MOD.Ares
{
    [Mod(
        name: "Ares",
        description: "Help you to play the arena",
        version: "0.1.0")]
    public class AresController : IMod
    {
        private const string _uiManagerPrefabPath = "AresUIManager";
        private AresContext _aresContext;
        private UIManager _uiManager;

        public bool IsInitialized => _aresContext != null;

        public void Initialize()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("AresController is already initialized");
                return;
            }

            _aresContext = new AresContext();

            var prefab = Resources.Load<UIManager>(_uiManagerPrefabPath);
            if (!prefab)
            {
                Debug.LogError($"Failed to load UI docs from {_uiManagerPrefabPath}");
                return;
            }

            _uiManager = Object.Instantiate(prefab);
            _uiManager.name = _uiManagerPrefabPath;
            _uiManager.Initialize(_aresContext);
        }

        public void Terminate()
        {
            _aresContext = null;
            if (_uiManager)
            {
                Object.Destroy(_uiManager);
                _uiManager = null;
            }
        }

        public void Show()
        {
            _aresContext.Track("9c_unity_mod_ares__show");
            if (!_uiManager)
            {
                return;
            }

            _aresContext.WinRates.Clear();
            _uiManager.Show();
        }
    }
}
