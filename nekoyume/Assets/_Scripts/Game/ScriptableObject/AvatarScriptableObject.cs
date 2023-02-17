using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game.ScriptableObject
{
    [CreateAssetMenu(fileName = "Avatar", menuName = "Scriptable Object/Avatar", order = int.MaxValue)]
    public class AvatarScriptableObject : UnityEngine.ScriptableObject
    {
        public List<SkeletonDataAsset> Body;
        public List<SkeletonDataAsset> FullCostume;
    }
}
