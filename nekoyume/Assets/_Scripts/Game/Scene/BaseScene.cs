using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nekoyume.Game.Scene
{
    public abstract class BaseScene : MonoBehaviour
    {
        public bool IsInitialized { get; protected set; } = false;

        /// <summary>
        /// 씬의 모든 오브젝트가 초기화된 뒤 씬 객체를 초기화하기 위해
        /// Awake가 아닌 Start타이밍에 초기화를 진행함
        /// </summary>
        protected virtual void Start()
        {
            WaitUntilInitialized().Forget();
        }

        private async UniTask WaitUntilInitialized()
        {
            await UniTask.WaitUntil(() => Game.instance.IsInitialized);
            await LoadSceneAssets();
            await WaitActionResponse();
            Initialize();
        }

        /// <summary>
        /// TODO: 어드레서블 적용시 해당 메서드에서 필요 에셋 로드
        /// </summary>
        protected abstract UniTask LoadSceneAssets();

        protected abstract UniTask WaitActionResponse();

        protected virtual void Initialize()
        {
            IsInitialized = true;
        }

        public abstract void Clear();
    }
}
