using System.Collections;
using UnityEngine;

namespace Nekoyume.Game.Util
{
    public abstract class BaseResourceAnimation : MonoBehaviour, IResourceAnimation
    {
        [Range(0, 60)] [SerializeField] private int fps = 60;
        [SerializeField] private bool loop;
        [SerializeField] private bool startAutomatically = true;

        [SerializeField] protected int startIndex = 0;
        private int _currentIndex;
        private protected int _frameCount;

        private Coroutine _coroutine;

        protected abstract void Init();
        protected abstract void Apply(int index);

        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            if (startIndex < 0 || startIndex >= _frameCount)
            {
                NcDebug.LogError($"Index is not exist : {startIndex}");
                return;
            }

            if (startAutomatically)
            {
                Play();
            }
        }

        public void Play()
        {
            _coroutine = StartCoroutine(PlayAnimation());
        }

        public void Stop()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
        }

        private IEnumerator PlayAnimation()
        {
            var wfs = 1.0f / (float) fps;

            var index = startIndex;
            while (true)
            {
                if (index >= _frameCount)
                {
                    if (!loop)
                    {
                        yield break;
                    }

                    index = 0;
                }

                Apply(index);
                yield return new WaitForSeconds(wfs);
                index++;
            }
        }
    }
}
