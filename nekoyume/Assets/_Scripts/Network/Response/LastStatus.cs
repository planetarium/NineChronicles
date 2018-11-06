namespace Nekoyume.Network.Response
{
    [System.Serializable]
    public class LastStatus : Base
    {
        public Model.Avatar avatar;
        public Model.BattleStatus[] status;

        public LastStatus()
        {
        }
    }
}
