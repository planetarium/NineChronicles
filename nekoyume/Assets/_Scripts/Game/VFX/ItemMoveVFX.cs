namespace Nekoyume.Game.VFX
{
    public class ItemMoveVFX: VFX
    {
        public ItemMoveVFX SetEmitDuration( float duration)
        {
            EmitDuration = duration;
            return this;
        }
    }
}
