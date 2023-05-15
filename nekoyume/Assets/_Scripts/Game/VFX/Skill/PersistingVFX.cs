namespace Nekoyume.Game.VFX.Skill
{
    public class PersistingVFX : BuffVFX
    {
        protected override float EmitDuration => 0f;
        public override bool IsPersisting => true;

        public override void Play()
        {
            base.Play();
        }
    }
}
