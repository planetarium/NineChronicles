namespace Nekoyume.Game.Character.Boss
{
    public class BossBase : Enemy
    {
        public void Awake()
        {
            _dyingTime = 2.5f;
        }
    }
}
