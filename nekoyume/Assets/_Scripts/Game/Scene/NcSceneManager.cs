using System;
using Cysharp.Threading.Tasks;
using Nekoyume.UI;
using Object = UnityEngine.Object;

namespace Nekoyume.Game.Scene
{    
    using UnityEngine.SceneManagement;
    
    public enum SceneType
    {
        Login,
        Loading,
        Game,
    }
    
    public class NcSceneManager
    {        
        private const float ChangeSceneDelay = 500f;
        
        private static class Singleton
        {
            internal static readonly NcSceneManager Value = new();
        }
        public static NcSceneManager Instance => Singleton.Value;

        public SceneType ESceneType { get; private set; } = SceneType.Login;

        private BaseScene _currentScene;

        public BaseScene CurrentScene => _currentScene;

        private NcSceneManager()
        {
            SceneManager.sceneUnloaded += OnSceneUnLoaded;
        }

        public async UniTask LoadScene(SceneType type)
        {
            // TODO: 씬 전환시 로딩 UI효과 추가
            // TODO: 씬 타입에 따라 로딩 스크린을 다르게 보여줄 수 있도록 수정
            // var loadingScreen = Widget.Find<LoadingScreen>();
            // loadingScreen.CloseWithOtherWidgets();
            // loadingScreen.Show(LoadingScreen.LoadingType.WorldBoss);

            ESceneType = type;
            ClearScene();
            await SceneManager.LoadSceneAsync(GetSceneName(SceneType.Loading)).ToUniTask();
            await UniTask.Delay(TimeSpan.FromMilliseconds(ChangeSceneDelay));
            await SceneManager.LoadSceneAsync(GetSceneName(type)).ToUniTask();
            await UniTask.NextFrame();
            _currentScene = Object.FindObjectOfType<BaseScene>();
            await UniTask.WaitUntil(() => _currentScene.IsInitialized);

            // Widget.Find<LoadingScreen>().Close();
        }

        // TODO: Addressable로 변경
        private string GetSceneName(SceneType type)
        {
            switch (type)
            {
                case SceneType.Login:
                    return "LoginScene";
                case SceneType.Loading:
                    return "LoadingScene";
                case SceneType.Game:
                    return "Game";
            }

            return string.Empty;
        }

        private void ClearScene()
        {
            if (_currentScene != null)
                _currentScene.Clear();
            _currentScene = null;

            // TODO: after apply addressable
            // resourceManager?.ReleaseAll();
        }

        private void OnSceneUnLoaded(Scene scene)
        {
        }
    }
}
