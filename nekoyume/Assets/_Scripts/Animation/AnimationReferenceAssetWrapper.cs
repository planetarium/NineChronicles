using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume
{
    public class AnimationReferenceAssetWrapper : AnimationReferenceAsset
    {
        public void SetReference(string animName, SkeletonDataAsset asset)
        {
            animationName = animName;
            skeletonDataAsset = asset;
            Initialize();
        }
    }
}
