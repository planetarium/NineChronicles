using System;
using Nekoyume.EnumType;
using Spine.Unity;

namespace Nekoyume.Game.Avatar
{
    [Serializable]
    public class AvatarParts
    {
        public AvatarPartsType Type;
        public SkeletonAnimation SkeletonAnimation;
    }
}
