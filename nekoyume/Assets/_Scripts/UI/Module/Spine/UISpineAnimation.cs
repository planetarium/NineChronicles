using System;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    [RequireComponent(typeof(SkeletonGraphic))]
    [RequireComponent(typeof(Button))]
    public class UISpineAnimation : MonoBehaviour
    {
        [SpineAnimation(dataField: "skeletonDataAsset")]
        public string startingAnimationName;

        [SpineAnimation(dataField: "skeletonDataAsset")]
        public string idleAnimationName;

        [SpineAnimation(dataField: "skeletonDataAsset")]
        public string onClickAnimationName;

        private SkeletonGraphic _seSkeletonGraphic;
        private Button _button;

        private void Awake()
        {
            _seSkeletonGraphic = GetComponent<SkeletonGraphic>();
            if (ReferenceEquals(_seSkeletonGraphic.skeletonDataAsset, null))
            {
                NcDebug.LogError("Skeleton Data Asset is null");
                return;
            }

            _button = GetComponent<Button>();
            _button.onClick.AddListener(() =>
            {
                _seSkeletonGraphic.AnimationState.SetAnimation(0, onClickAnimationName, false);
                _seSkeletonGraphic.AnimationState.AddAnimation(0, idleAnimationName, true, 0f);
            });
        }

        private void OnEnable()
        {
            _seSkeletonGraphic.AnimationState.SetAnimation(0, startingAnimationName, false);
            _seSkeletonGraphic.AnimationState.AddAnimation(0, idleAnimationName, true, 0f);
        }
    }
}
