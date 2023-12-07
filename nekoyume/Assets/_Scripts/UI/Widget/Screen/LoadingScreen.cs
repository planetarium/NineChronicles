using System;
using System.Linq;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    using UniRx;
    public class LoadingScreen : ScreenWidget
    {
        public enum LoadingType
        {
            None,
            Entering,
            Adventure,
            Arena,
            Shop,
            Workshop,
            WorldBoss,
            JustModule,
            WorldUnlock,
        }

        [Serializable]
        private class BackgroundItem
        {
            public LoadingType type;
            public VideoClip videoClip;
            public Texture texture;
        }

        [SerializeField] private LoadingModule loadingModule;
        [Space]
        [SerializeField] private GameObject animationContainer;
        [SerializeField] private RawImage imageContainer;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Texture videoTexture;

        [SerializeField] private BackgroundItem[] backgroundItems;

        protected override void Awake()
        {
            base.Awake();

            var pos = transform.localPosition;
            pos.z = -5f;
            transform.localPosition = pos;

            loadingModule.Initialize();
        }

        public void Show(
            LoadingType loadingType = LoadingType.None,
            string message = null,
            bool autoClose = false,
            bool ignoreShowAnimation = false)
        {
            if (autoClose)
            {
                Observable.Timer(TimeSpan.FromSeconds(3))
                    .Subscribe(_ => Close()).AddTo(gameObject);
            }

            SetBackGround(loadingType);

            base.Show(ignoreShowAnimation);
            loadingModule.Show(
                message,
                loadingType == LoadingType.JustModule,
                loadingType == LoadingType.JustModule);
        }

        private void SetBackGround(LoadingType type)
        {
            if (type == LoadingType.JustModule)
            {
                animationContainer.SetActive(false);
                imageContainer.gameObject.SetActive(false);
                return;
            }

            var item = backgroundItems.FirstOrDefault(item => item.type == type);

            var playVideo = item != null;
            animationContainer.SetActive(!playVideo);
            imageContainer.gameObject.SetActive(playVideo);

            if (playVideo)
            {
                imageContainer.texture = item.texture;
                var clip = item.videoClip;

                if (clip)
                {
                    videoPlayer.clip = clip;
                    videoPlayer.Play();
                    imageContainer.texture = videoTexture;
                }
            }
        }
    }
}
