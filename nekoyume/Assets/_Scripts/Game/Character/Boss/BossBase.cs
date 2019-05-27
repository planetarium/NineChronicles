namespace Nekoyume.Game.Character.Boss
{
    public class BossBase : Enemy
    {
        protected override void Awake()
        {
            base.Awake();
            
            dyingTime = 2.5f;
        }
    }
}
