namespace Nekoyume.Game.Character.Boss
{
    public class BossBase : Enemy
    {
        protected override void Awake()
        {
            base.Awake();
            
            _dyingTime = 2.5f;
        }
    }
}
