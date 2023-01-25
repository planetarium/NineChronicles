using System;
using Nekoyume.EnumType;
using Spine.Unity;

namespace Nekoyume.Game.Character
{
    [Serializable]
    public class AvatarParts
    {
        public AvatarPartsType Type;
        public SkeletonAnimation SkeletonAnimation;
    }
}
