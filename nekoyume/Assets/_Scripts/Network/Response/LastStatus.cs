namespace Nekoyume.Network.Response
{
    [System.Serializable]
    public class LastStatus : Base
    {
        public Avatar avatar;
        public BattleStatus[] status;

        public LastStatus()
        {
        }
    }
}
