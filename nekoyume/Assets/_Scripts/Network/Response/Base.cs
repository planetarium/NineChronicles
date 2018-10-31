namespace Nekoyume.Network.Response
{
    [System.Serializable]
    public class Base
    {
        public ResultCode result = ResultCode.OK;
        public string message = "";

        public Base()
        {
        }
    }
}
