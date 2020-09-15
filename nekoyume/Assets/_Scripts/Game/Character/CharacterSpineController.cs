namespace Nekoyume.Game.Character
{
    public class CharacterSpineController : SpineController
    {
        protected override bool IsLoopAnimation(string animationName)
        {
            return animationName == nameof(CharacterAnimation.Type.Idle)
                   || animationName == nameof(CharacterAnimation.Type.Run)
                   || animationName == nameof(CharacterAnimation.Type.Casting)
                   || animationName == nameof(CharacterAnimation.Type.TurnOver_01);
        }
    }
}
