using Spine;

namespace Nekoyume.Game.Character
{
    public class NPCSpineController : SpineController
    {
        protected override bool IsLoopAnimation(string animationName)
        {
            return animationName == nameof(NPCAnimation.Type.Idle)
                   || animationName == nameof(NPCAnimation.Type.Idle_02)
                   || animationName == nameof(NPCAnimation.Type.Idle_03)
                   || animationName == nameof(NPCAnimation.Type.Loop)
                   || animationName == nameof(NPCAnimation.Type.Loop_02)
                   || animationName == nameof(NPCAnimation.Type.Loop_03)
                   || animationName == nameof(CharacterAnimation.Type.Idle)
                   || animationName == nameof(NPCAnimation.Type.Over);
        }
    }
}
