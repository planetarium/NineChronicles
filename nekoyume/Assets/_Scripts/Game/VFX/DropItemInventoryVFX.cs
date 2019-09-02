namespace Nekoyume.Game.VFX
{
    public class DropItemInventoryVFX : VFX
    {
        public override void Play()
        {
            base.Play();
            _particlesRoot.Play(true);
        }
    }
}
