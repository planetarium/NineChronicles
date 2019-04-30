using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.UI;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class PlayerAnimator : MecanimCharacterAnimator
    {
        private const string StringHUD = "HUD";
        
        private SkeletonAnimation skeleton { get; set; }

        private Vector3 hudPosition { get; set; }
        
        public PlayerAnimator(CharacterBase root) : base(root)
        {
        }

        public override void ResetTarget(GameObject value)
        {
            base.ResetTarget(value);
            
            if (!ReferenceEquals(skeleton, null))
            {
                skeleton.AnimationState.Event -= RaiseEvent;
            }

            skeleton = value.GetComponent<SkeletonAnimation>(); 

            if (ReferenceEquals(skeleton, null))
            {
                throw new NotFoundComponentException<SkeletonAnimation>();
            }

            var hud = skeleton.skeleton.FindBone(StringHUD);
            if (ReferenceEquals(hud, null))
            {
                throw new SpineBoneNotFoundException(StringHUD);
            }

            hudPosition = hud.GetWorldPosition(target.transform) - root.transform.position;
            
            skeleton.AnimationState.Event += RaiseEvent;
        }

        public override Vector3 GetHUDPosition()
        {
            return hudPosition;
        }

        private void RaiseEvent(TrackEntry trackEntry, Spine.Event e)
        {
            onEvent.OnNext(e.Data.Name);
        }
    }
}
