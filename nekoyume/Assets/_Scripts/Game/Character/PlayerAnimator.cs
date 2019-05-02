using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.UI;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class PlayerAnimator : SkeletonCharacterAnimator
    {
        public PlayerAnimator(CharacterBase root) : base(root)
        {
        }   
    }
}
